using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Game.Gameplay.Tests
{
    public class LevelValidatorTests
    {
        // Build levels on inactive GameObjects so Awake (which would create Sim/tweens) never
        // runs in EditMode — same trick as AirCushionCooldownTests. We only need the component
        // graph, which GetComponentsInChildren(includeInactive:true) reads fine on inactive roots.

        readonly List<Object> _toCleanUp = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _toCleanUp.Count; i++)
            {
                if (_toCleanUp[i] != null) Object.DestroyImmediate(_toCleanUp[i]);
            }
            _toCleanUp.Clear();
        }

        Level BuildLevel(int ropes = 1, bool candy = true, bool omNoms = 1 == 1, bool driver = true, string id = "level-test")
        {
            var go = new GameObject("TestLevel");
            go.SetActive(false);
            _toCleanUp.Add(go);
            var level = go.AddComponent<Level>();
            SetId(level, id);

            var driverGo = new GameObject("Driver");
            driverGo.transform.SetParent(go.transform, false);
            _toCleanUp.Add(driverGo);
            if (driver) driverGo.AddComponent<RopeSimulationDriver>();

            for (int i = 0; i < ropes; i++)
            {
                var ropeGo = new GameObject($"Rope{i}");
                ropeGo.transform.SetParent(driverGo.transform, false);
                _toCleanUp.Add(ropeGo);
                ropeGo.AddComponent<RopeAuthoring>();
            }

            if (candy)
            {
                var candyGo = new GameObject("Candy");
                candyGo.transform.SetParent(go.transform, false);
                _toCleanUp.Add(candyGo);
                candyGo.AddComponent<CandyFollower>();
            }

            if (omNoms)
            {
                var mouthGo = new GameObject("Mouth");
                mouthGo.transform.SetParent(go.transform, false);
                _toCleanUp.Add(mouthGo);
                mouthGo.AddComponent<OmNom>();
            }

            return level;
        }

        static void SetId(Level level, string id)
        {
            var f = typeof(Level).GetField("levelId", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "Level.levelId field not found — rename?");
            f.SetValue(level, id);
        }

        LevelCatalog MakeCatalog(params Level[] levels)
        {
            var catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            _toCleanUp.Add(catalog);
            var box = new LevelCatalog.Box
            {
                id = "box-test",
                displayName = "Test Box",
                levels = levels,
                starsToUnlock = 0,
            };
            var f = typeof(LevelCatalog).GetField("boxes", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "LevelCatalog.boxes field not found — rename?");
            f.SetValue(catalog, new[] { box });
            return catalog;
        }

        // ── per-level ──────────────────────────────────────────────────────

        [Test]
        public void ValidLevel_HasNoErrors()
        {
            Level level = BuildLevel(ropes: 2, candy: true, omNoms: true, driver: true, id: "abc123");
            var report = LevelValidator.ValidateLevel(level);
            Assert.IsTrue(report.Ok, "well-formed level should pass: " + string.Join("; ", report.Errors));
        }

        [Test]
        public void MissingCandy_IsFlagged()
        {
            Level level = BuildLevel(candy: false);
            var report = LevelValidator.ValidateLevel(level);
            Assert.IsFalse(report.Ok);
            Assert.That(string.Join(" ", report.Errors), Does.Contain("candy").IgnoreCase);
        }

        [Test]
        public void MissingOmNom_IsFlagged()
        {
            Level level = BuildLevel(omNoms: false);
            var report = LevelValidator.ValidateLevel(level);
            Assert.IsFalse(report.Ok);
            Assert.That(string.Join(" ", report.Errors), Does.Contain("Om Nom").IgnoreCase);
        }

        [Test]
        public void TwoOmNom_IsFlagged()
        {
            var go = new GameObject("TwoMouths");
            go.SetActive(false);
            _toCleanUp.Add(go);
            var level = go.AddComponent<Level>();
            SetId(level, "lvl");
            var driverGo = new GameObject("Driver");
            driverGo.transform.SetParent(go.transform, false);
            _toCleanUp.Add(driverGo);
            driverGo.AddComponent<RopeSimulationDriver>();
            var ropeGo = new GameObject("Rope");
            ropeGo.transform.SetParent(driverGo.transform, false);
            _toCleanUp.Add(ropeGo);
            ropeGo.AddComponent<RopeAuthoring>();
            var candyGo = new GameObject("Candy");
            candyGo.transform.SetParent(go.transform, false);
            _toCleanUp.Add(candyGo);
            candyGo.AddComponent<CandyFollower>();
            var m1 = new GameObject("Mouth1"); m1.transform.SetParent(go.transform, false); m1.AddComponent<OmNom>(); _toCleanUp.Add(m1);
            var m2 = new GameObject("Mouth2"); m2.transform.SetParent(go.transform, false); m2.AddComponent<OmNom>(); _toCleanUp.Add(m2);

            var report = LevelValidator.ValidateLevel(level);
            Assert.IsFalse(report.Ok);
            Assert.That(string.Join(" ", report.Errors), Does.Contain("exactly one"));
        }

        [Test]
        public void NoRopes_IsFlagged()
        {
            Level level = BuildLevel(ropes: 0);
            var report = LevelValidator.ValidateLevel(level);
            Assert.IsFalse(report.Ok);
            Assert.That(string.Join(" ", report.Errors), Does.Contain("rope").IgnoreCase);
        }

        [Test]
        public void EmptyId_IsFlagged()
        {
            Level level = BuildLevel(id: "");
            var report = LevelValidator.ValidateLevel(level);
            Assert.IsFalse(report.Ok);
            Assert.That(string.Join(" ", report.Errors), Does.Contain("id").IgnoreCase);
        }

        [Test]
        public void MissingDriver_IsFlagged()
        {
            Level level = BuildLevel(driver: false);
            var report = LevelValidator.ValidateLevel(level);
            Assert.IsFalse(report.Ok);
            Assert.That(string.Join(" ", report.Errors), Does.Contain("Driver").IgnoreCase);
        }

        // ── catalog ────────────────────────────────────────────────────────

        [Test]
        public void Catalog_DuplicateIds_AreFlagged()
        {
            Level a = BuildLevel(id: "dup-id");
            Level b = BuildLevel(id: "dup-id");
            LevelCatalog catalog = MakeCatalog(a, b);

            var report = LevelValidator.ValidateCatalog(catalog);
            Assert.IsFalse(report.Ok);
            Assert.That(string.Join(" ", report.Errors), Does.Contain("Duplicate level id"));
        }

        [Test]
        public void Catalog_NullEntry_IsFlagged()
        {
            Level a = BuildLevel(id: "good-id");
            LevelCatalog catalog = MakeCatalog(a, null);

            var report = LevelValidator.ValidateCatalog(catalog);
            Assert.IsFalse(report.Ok);
            Assert.That(string.Join(" ", report.Errors), Does.Contain("null level prefab").IgnoreCase);
        }

        [Test]
        public void Catalog_ValidLevels_HasNoErrors()
        {
            Level a = BuildLevel(id: "id-a");
            Level b = BuildLevel(id: "id-b");
            LevelCatalog catalog = MakeCatalog(a, b);

            var report = LevelValidator.ValidateCatalog(catalog);
            Assert.IsTrue(report.Ok, string.Join("; ", report.Errors));
        }
    }
}
