using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimConversionUnitSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var source = new IfcStepStandaloneSource();
            var document = source.Parse("conversion-qto.ifc", Step());
            var qto = document.Elements.Single().Quantities;

            Near(3.048d, qto.Single(x => x.QuantityName == "Length").Quantity, 1e-12, "global foot length to metre");
            Near(9.290304d, qto.Single(x => x.QuantityName == "Area").Quantity, 1e-12, "explicit square-foot area to square metre");
            Near(0.28316846592d, qto.Single(x => x.QuantityName == "Volume").Quantity, 1e-12, "global cubic-foot volume to cubic metre");
            Near(1.25d, qto.Single(x => x.QuantityName == "Mass").Quantity, 1e-12, "SI gram mass remains compatible");
            Near(0.3048d, qto.Single(x => x.QuantityName == "NestedInch").Quantity, 1e-12, "nested inch through foot chain");
            Equal("m", qto.Single(x => x.QuantityName == "Length").Unit, "length canonical unit");
            Equal("m2", qto.Single(x => x.QuantityName == "Area").Unit, "area canonical unit");
            Equal("m3", qto.Single(x => x.QuantityName == "Volume").Unit, "volume canonical unit");

            Reject(() => source.Parse("cycle.ifc", Step().Replace("IFCLENGTHMEASURE(0.0833333333333333),#230", "IFCLENGTHMEASURE(0.0833333333333333),#233")), "conversion cycle fails closed");
            Reject(() => source.Parse("dimension.ifc", Step().Replace("#201=IFCDIMENSIONALEXPONENTS(2,0,0,0,0,0,0)", "#201=IFCDIMENSIONALEXPONENTS(1,0,0,0,0,0,0)")), "area dimension mismatch fails closed");
            Reject(() => source.Parse("factor.ifc", Step().Replace("IFCLENGTHMEASURE(0.3048)", "IFCLENGTHMEASURE(0)")), "non-positive conversion factor fails closed");
            Reject(() => source.Parse("measure-type.ifc", Step().Replace("IFCAREAMEASURE(0.09290304)", "IFCLENGTHMEASURE(0.09290304)")), "conversion measure type mismatch fails closed");
            Reject(() => source.Parse("missing-factor.ifc", Step().Replace("'SQUARE_FOOT',#221", "'SQUARE_FOOT',#999")), "dangling conversion factor fails closed");
        }

        private static string Step()
        {
            return "ISO-10303-21;\nHEADER;\nFILE_SCHEMA(('IFC4'));\nENDSEC;\nDATA;\n" +
                "#1=IFCPROJECT('P',$,'Conversion project',$,$,$,$,$,#90);\n" +
                "#10=IFCWALL('G-CONV',$,'Converted wall',$,$,$,$,$);\n" +
                "#50=IFCQUANTITYLENGTH('Length',$,$,10.0,$);\n" +
                "#51=IFCQUANTITYAREA('Area',$,#231,100.0,$);\n" +
                "#52=IFCQUANTITYVOLUME('Volume',$,$,10.0,$);\n" +
                "#53=IFCQUANTITYWEIGHT('Mass',$,$,1250.0,$);\n" +
                "#54=IFCQUANTITYLENGTH('NestedInch',$,#233,12.0,$);\n" +
                "#55=IFCELEMENTQUANTITY('Q',$,'BaseQuantities',$,$,(#50,#51,#52,#53,#54));\n" +
                "#56=IFCRELDEFINESBYPROPERTIES('R',$,$,$,(#10),#55);\n" +
                "#200=IFCDIMENSIONALEXPONENTS(1,0,0,0,0,0,0);\n" +
                "#201=IFCDIMENSIONALEXPONENTS(2,0,0,0,0,0,0);\n" +
                "#202=IFCDIMENSIONALEXPONENTS(3,0,0,0,0,0,0);\n" +
                "#210=IFCSIUNIT(*,.LENGTHUNIT.,$,.METRE.);\n" +
                "#211=IFCSIUNIT(*,.AREAUNIT.,$,.SQUARE_METRE.);\n" +
                "#212=IFCSIUNIT(*,.VOLUMEUNIT.,$,.CUBIC_METRE.);\n" +
                "#213=IFCSIUNIT(*,.MASSUNIT.,$,.GRAM.);\n" +
                "#220=IFCMEASUREWITHUNIT(IFCLENGTHMEASURE(0.3048),#210);\n" +
                "#221=IFCMEASUREWITHUNIT(IFCAREAMEASURE(0.09290304),#211);\n" +
                "#222=IFCMEASUREWITHUNIT(IFCVOLUMEMEASURE(0.028316846592),#212);\n" +
                "#223=IFCMEASUREWITHUNIT(IFCLENGTHMEASURE(0.0833333333333333),#230);\n" +
                "#230=IFCCONVERSIONBASEDUNIT(#200,.LENGTHUNIT.,'FOOT',#220);\n" +
                "#231=IFCCONVERSIONBASEDUNIT(#201,.AREAUNIT.,'SQUARE_FOOT',#221);\n" +
                "#232=IFCCONVERSIONBASEDUNIT(#202,.VOLUMEUNIT.,'CUBIC_FOOT',#222);\n" +
                "#233=IFCCONVERSIONBASEDUNIT(#200,.LENGTHUNIT.,'INCH',#223);\n" +
                "#90=IFCUNITASSIGNMENT((#230,#211,#232,#213));\nENDSEC;\nEND-ISO-10303-21;\n";
        }

        private static void Reject(System.Action action, string label)
        {
            var failed = false;
            try { action(); }
            catch (InvalidDataException) { failed = true; }
            if (!failed) throw new InvalidOperationException(label + ": expected InvalidDataException.");
        }

        private static void Near(double expected, double actual, double tolerance, string label)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
