using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimPropertyValueSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var source = new IfcStepStandaloneSource();
            var document = source.Parse("property-values.ifc", PositiveStep());
            var element = document.Elements.Single(x => x.Guid == "G-PROP");
            var properties = element.Properties.ToDictionary(x => x.Name, x => x.Value, StringComparer.OrdinalIgnoreCase);

            Equal(9, properties.Count, "flattened property count");
            Equal("Tile", properties["Pset_QS.Finish[0000000000]"], "enumerated value 0");
            Equal("Paint", properties["Pset_QS.Finish[0000000001]"], "enumerated value 1");
            Equal("A", properties["Pset_QS.Phases[0000000000]"], "list value 0");
            Equal("B", properties["Pset_QS.Phases[0000000001]"], "list value 1");
            Equal("18", properties["Pset_QS.Temperature.Lower"], "bounded lower");
            Equal("30", properties["Pset_QS.Temperature.Upper"], "bounded upper");
            Equal("22", properties["Pset_QS.Temperature.SetPoint"], "bounded set point");
            Equal("W-01", properties["Pset_QS.Code"], "single-value compatibility");
            Equal(string.Empty, properties["Pset_QS.EmptyList"], "nil list property presence");

            Throws<InvalidDataException>(() => source.Parse("heterogeneous-list.ifc", InvalidPropertyStep("#20=IFCPROPERTYLISTVALUE('Mixed',$,(IFCLABEL('A'),IFCINTEGER(2)),$);", "#20")), "heterogeneous list");
            Throws<InvalidDataException>(() => source.Parse("invalid-enum.ifc", InvalidEnumerationMembershipStep()), "enumeration membership");
            Throws<InvalidDataException>(() => source.Parse("mixed-bounds.ifc", InvalidPropertyStep("#20=IFCPROPERTYBOUNDEDVALUE('MixedBounds',$,IFCREAL(3.),IFCINTEGER(1),$,$);", "#20")), "heterogeneous bounds");
            Throws<InvalidDataException>(() => source.Parse("unsupported-property.ifc", InvalidPropertyStep("#20=IFCPROPERTYREFERENCEVALUE('Ref',$,$,#10);", "#20")), "unsupported property kind");
        }

        private static string PositiveStep()
        {
            return Header() +
                "#10=IFCWALL('G-PROP',$,'Wall',$,$,$,$,$);\n" +
                "#20=IFCPROPERTYENUMERATION('FinishEnum',(IFCLABEL('Paint'),IFCLABEL('Tile')),$);\n" +
                "#21=IFCPROPERTYENUMERATEDVALUE('Finish',$,(IFCLABEL('Tile'),IFCLABEL('Paint')),#20);\n" +
                "#22=IFCPROPERTYLISTVALUE('Phases',$,(IFCIDENTIFIER('A'),IFCIDENTIFIER('B')),$);\n" +
                "#23=IFCPROPERTYBOUNDEDVALUE('Temperature',$,IFCREAL(30.),IFCREAL(18.),$,IFCREAL(22.));\n" +
                "#24=IFCPROPERTYSINGLEVALUE('Code',$,IFCTEXT('W-01'),$);\n" +
                "#25=IFCPROPERTYLISTVALUE('EmptyList',$,$,$);\n" +
                "#30=IFCPROPERTYSET('PSET',$,'Pset_QS',$,(#21,#22,#23,#24,#25));\n" +
                "#31=IFCRELDEFINESBYPROPERTIES('REL',$,$,$,(#10),#30);\n" + Footer();
        }

        private static string InvalidEnumerationMembershipStep()
        {
            return Header() +
                "#10=IFCWALL('G-BAD-ENUM',$,'Wall',$,$,$,$,$);\n" +
                "#20=IFCPROPERTYENUMERATION('FinishEnum',(IFCLABEL('Paint'),IFCLABEL('Tile')),$);\n" +
                "#21=IFCPROPERTYENUMERATEDVALUE('Finish',$,(IFCLABEL('Stone')),#20);\n" +
                "#30=IFCPROPERTYSET('PSET',$,'Pset_QS',$,(#21));\n" +
                "#31=IFCRELDEFINESBYPROPERTIES('REL',$,$,$,(#10),#30);\n" + Footer();
        }

        private static string InvalidPropertyStep(string propertyRecord, string propertyRef)
        {
            return Header() +
                "#10=IFCWALL('G-BAD',$,'Wall',$,$,$,$,$);\n" + propertyRecord + "\n" +
                "#30=IFCPROPERTYSET('PSET',$,'Pset_QS',$,(" + propertyRef + "));\n" +
                "#31=IFCRELDEFINESBYPROPERTIES('REL',$,$,$,(#10),#30);\n" + Footer();
        }

        private static string Header()
        {
            return "ISO-10303-21;\nHEADER;\nFILE_SCHEMA(('IFC4'));\nENDSEC;\nDATA;\n";
        }

        private static string Footer()
        {
            return "ENDSEC;\nEND-ISO-10303-21;\n";
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Throws<T>(Action action, string label) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException(label + ": expected " + typeof(T).Name + ".");
        }
    }
}
