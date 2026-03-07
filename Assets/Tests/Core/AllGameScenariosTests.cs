using System.Collections.Generic;
using NUnit.Framework;
using Unidad.Core.Bootstrap;
using Unidad.Core.Testing;

namespace TextRPG.Tests
{
    /// <summary>
    /// Discovers and runs ALL scenarios from ALL game system installers in TextRPG.Core.
    /// Mirrors AllSystemScenariosTests from Unidad.Core.Tests but scoped to game assemblies.
    /// </summary>
    [TestFixture]
    public class AllGameScenariosTests
    {
        private static IEnumerable<TestCaseData> AllScenarios()
        {
            foreach (var installerType in InstallerDiscovery.FindInstallerTypes())
            {
                if (!installerType.Namespace?.StartsWith("TextRPG") ?? true)
                    continue;

                var installer = InstallerDiscovery.CreateInstaller(installerType);
                if (installer == null) continue;

                var factory = installer.CreateTestFactory();
                if (factory == null) continue;

                foreach (var scenario in factory.GetScenarios())
                {
                    yield return new TestCaseData(scenario)
                        .SetName($"{installerType.Name} > {scenario.Definition.Name}")
                        .SetDescription(scenario.Definition.Description);
                }
            }
        }

        [TestCaseSource(nameof(AllScenarios))]
        public void Scenario_Passes(ITestScenario scenario)
        {
            scenario.Execute();
            var result = scenario.Verify();

            Assert.That(result.Success, Is.True,
                result.FailureMessage ?? "Scenario failed with no message");
        }
    }
}
