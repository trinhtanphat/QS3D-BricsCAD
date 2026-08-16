using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyImpactTopologyFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsInPlaceDependencyMutationDuringRootEnumeration();
            PreservesOrdinaryDependencyPlanning();
        }

        private static void RejectsInPlaceDependencyMutationDuringRootEnumeration()
        {
            var project = CreateProject();
            var dependent = project.Elements[1];
            var version = project.ChangeVersion;
            var roots = new MutatingRoots("ROOT", () => dependent.DependsOn.Clear());

            var threw = false;
            try
            {
                new DependencyImpactPlanner().Plan(project, roots);
            }
            catch (InvalidOperationException ex)
            {
                threw = ex.Message.IndexOf("dependency topology changed", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (!threw)
                throw new InvalidOperationException("Dependency impact planning must fail closed when DependsOn mutates in place during root enumeration.");
            if (project.ChangeVersion != version)
                throw new InvalidOperationException("The adversarial direct DependsOn mutation must demonstrate that ChangeVersion alone cannot detect topology freshness.");
        }

        private static void PreservesOrdinaryDependencyPlanning()
        {
            var project = CreateProject();
            var plan = new DependencyImpactPlanner().Plan(project, new[] { "ROOT" });
            if (plan.TotalCount != 1 || plan.Entries[0].ElementId != "DEPENDENT" || plan.Entries[0].Depth != 1)
                throw new InvalidOperationException("Canonical dependency impact planning changed while adding topology freshness checks.");
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("P1", "Dependency freshness smoke");
            var root = new ProjectElement("ROOT", ElementCategory.CustomQuantity);
            var dependent = new ProjectElement("DEPENDENT", ElementCategory.CustomQuantity);
            dependent.DependsOn.Add("ROOT");
            project.Elements.Add(root);
            project.Elements.Add(dependent);
            return project;
        }

        private sealed class MutatingRoots : IEnumerable<string>
        {
            private readonly string _root;
            private readonly Action _mutation;

            public MutatingRoots(string root, Action mutation)
            {
                _root = root;
                _mutation = mutation;
            }

            public IEnumerator<string> GetEnumerator()
            {
                yield return _root;
                _mutation();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
