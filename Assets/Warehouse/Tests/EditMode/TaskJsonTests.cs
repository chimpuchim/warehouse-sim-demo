using System.IO;
using NUnit.Framework;
using UnityEngine;
using Warehouse;

namespace Warehouse.Tests
{
    public class TaskJsonTests
    {
        static string TasksDir => Path.Combine(Application.streamingAssetsPath, TaskLibrary.Folder);

        [Test]
        public void ParsesAFullTaskFile()
        {
            var def = TaskLibrary.Parse(File.ReadAllText(Path.Combine(TasksDir, "pack_order_2077.json")));
            Assert.AreEqual("pack_2077", def.id);
            Assert.AreEqual(TaskDefinition.Pack, def.type);
            Assert.AreEqual("pack_box_a", def.targetZone);
            Assert.IsTrue(def.requireClosed);
            Assert.AreEqual(2, def.requiredItems.Length);
            Assert.AreEqual("part", def.requiredItems[0].item);
            Assert.AreEqual(2, def.requiredItems[0].count);
            Assert.IsEmpty(def.Validate());
        }

        [Test]
        public void EveryShippedTaskIsValid()
        {
            var manifest = TaskLibrary.ParseManifest(File.ReadAllText(Path.Combine(TasksDir, TaskLibrary.ManifestFile)));
            Assert.GreaterOrEqual(manifest.tasks.Length, 3);
            foreach (var file in manifest.tasks)
            {
                var def = TaskLibrary.Parse(File.ReadAllText(Path.Combine(TasksDir, file)));
                Assert.IsEmpty(def.Validate(), file);
            }
        }

        [Test]
        public void ValidationReportsBadTypeItemAndCount()
        {
            var def = TaskLibrary.Parse("{\"id\":\"x\",\"type\":\"SORT\",\"targetZone\":\"tote_a\",\"timeLimitSeconds\":10,\"requiredItems\":[{\"item\":\"anvil\",\"count\":0}]}");
            var errors = def.Validate();
            Assert.AreEqual(3, errors.Count, string.Join("; ", errors));
            StringAssert.Contains("SORT", errors[0]);
            StringAssert.Contains("anvil", errors[1]);
            StringAssert.Contains("count", errors[2]);
        }

        [Test]
        public void ScoreAddsBonusPerFullSecondLeft()
        {
            var def = new TaskDefinition { timeLimitSeconds = 120, baseScore = 100, timeBonusPerSecond = 2 };
            Assert.AreEqual(100 + 2 * 97, Scoring.Compute(def, 22.4f));
            Assert.AreEqual(100, Scoring.Compute(def, 130f));
        }
    }
}
