# PowerShell 7: execute actual production normalization, without CAD or UI.
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$source = Get-Content (Join-Path $taskRoot 'src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs') -Raw -Encoding UTF8
function Read-NormalizationMethod([string]$Name) {
    $match=[regex]::Match($source,'(?m)^        private (?:static )?(?:string|bool) '+$Name+'\(')
    if(-not $match.Success){throw "Missing production method $Name"}
    $brace=$source.IndexOf('{',$match.Index);$depth=0
    for($i=$brace;$i -lt $source.Length;$i++){
        if($source[$i] -eq '{'){$depth++}
        elseif($source[$i] -eq '}'){$depth--;if($depth -eq 0){return $source.Substring($match.Index,$i-$match.Index+1)}}
    }
    throw "Unterminated method $Name"
}
$methods=(@('NormalizePropertyValue','IsBooleanProperty','IsNumericProperty','TryBoolean','TryFiniteNumber','RequiresPositiveNumber','RequiresNonNegativeNumber') | ForEach-Object {Read-NormalizationMethod $_}) -join "`n"
$classifier=Get-Content (Join-Path $taskRoot 'src/QS3D.Core/Domain/SemanticPropertyUnitClassifier.cs') -Raw -Encoding UTF8
$contract=Get-Content (Join-Path $taskRoot 'src/QS3D.BricsCAD.V25/SingleFootingContract.cs') -Raw -Encoding UTF8
$dimensionMethod=[regex]::Match($contract,'public static bool IsDimensionKey\(string\? key\)\s*\{[^}]+\}').Value
if(-not $dimensionMethod){throw 'Missing production dimension-key classification'}
$constants=([regex]::Matches($contract,'public const string [LWH][12]Key = "[^"]+";') | ForEach-Object {$_.Value}) -join "`n"
$fixture=@'
#nullable enable
using System;
using System.Linq;
using System.Globalization;
using QS3D.Core.Domain;
public class FootingNormalizationFixture {
    string Status = "";
    static string DisplayNameFor(string key) => key;
    static bool UsesMillimeterPresentation(string key) => SemanticPropertyUnitClassifier.IsLinearMeterProperty(key);
    public static void Run(){
        var p=new FootingNormalizationFixture();
        foreach(var field in new[]{"L1","W1","L2","W2","H1","H2"})
        foreach(var key in new[]{"SINGLE_FOOTING_"+field,"SingleFooting"+field+"M"})
        foreach(var value in new[]{"0","1","2","0.125"}) {
            bool valid;
            var actual=p.NormalizePropertyValue(key,"",value,value,out valid);
            if(!valid || actual!=value || IsBooleanProperty(key,value)) throw new Exception(key+" changed "+value+" to "+actual);
            actual=p.NormalizePropertyValue(key,"",value,"true",out valid);
            if(valid || actual!=value) throw new Exception(key+" accepted a boolean dimension");
        }
        foreach(var pair in new[]{new[]{"0","false"},new[]{"1","true"},new[]{"true","true"},new[]{"false","false"}}){
            bool valid;var actual=p.NormalizePropertyValue("Enabled","",pair[0],pair[0],out valid);
            if(!valid || actual!=pair[1]) throw new Exception("Ordinary boolean behavior regressed");
        }
        bool ordinaryValid;
        if(p.NormalizePropertyValue("LengthM","mm","1","1000",out ordinaryValid)!="1" || !ordinaryValid) throw new Exception("Ordinary meter/mm behavior regressed");
    }
// METHODS
}
public static class SingleFootingContract {
// CONSTANTS
// DIMENSION_METHOD
}
'@
# The fixture already imports System; keep the classifier's body unchanged.
Add-Type -TypeDefinition ($fixture.Replace('// METHODS',$methods).Replace('// CONSTANTS',$constants).Replace('// DIMENSION_METHOD',$dimensionMethod) + "`n" + $classifier.Replace('using System;',''))
[FootingNormalizationFixture]::Run()
Write-Output 'PASS: production normalization preserves 12 canonical/legacy numeric keys, rejects booleans, retains ordinary boolean and mm behavior.'
