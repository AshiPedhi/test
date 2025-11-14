using NUnit.Framework;
using UnityEngine;
using ChunaVR.PoseData;
using Oculus.Interaction.Input;
using System.Collections.Generic;

namespace ChunaVR.Tests
{
    /// <summary>
    /// HandPoseComparer 단위 테스트
    /// </summary>
    public class HandPoseComparerTests
    {
        private HandPoseConfig config;
        private HandPoseComparer comparer;

        [SetUp]
        public void Setup()
        {
            // 기본 설정으로 초기화
            config = HandPoseConfig.Default();
            comparer = new HandPoseComparer(config);
        }

        [Test]
        public void Constructor_WithValidConfig_ShouldNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => new HandPoseComparer(config));
        }

        [Test]
        public void UpdateConfig_WithNewConfig_ShouldUpdateSuccessfully()
        {
            // Arrange
            var newConfig = HandPoseConfig.Default();
            newConfig.positionThreshold = 0.1f;

            // Act
            comparer.UpdateConfig(newConfig);
            var retrievedConfig = comparer.GetConfig();

            // Assert
            Assert.AreEqual(0.1f, retrievedConfig.positionThreshold);
        }

        [Test]
        public void SetReferencePoint_WithValidTransform_ShouldNotThrow()
        {
            // Arrange
            var go = new GameObject("Reference");
            var transform = go.transform;

            // Act & Assert
            Assert.DoesNotThrow(() => comparer.SetReferencePoint(transform));

            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void GetConfig_AfterConstruction_ShouldReturnSameConfig()
        {
            // Act
            var retrievedConfig = comparer.GetConfig();

            // Assert
            Assert.IsNotNull(retrievedConfig);
            Assert.AreEqual(config.positionThreshold, retrievedConfig.positionThreshold);
            Assert.AreEqual(config.rotationThreshold, retrievedConfig.rotationThreshold);
        }

        [Test]
        public void CompareJointPose_WithNullHand_ShouldReturnZeroSimilarity()
        {
            // Arrange
            var replayPoses = new Dictionary<int, PoseData>();

            // Act
            var result = comparer.CompareJointPose(null, replayPoses, "TestHand");

            // Assert
            Assert.AreEqual(0f, result.similarity);
            Assert.IsFalse(result.passed);
        }

        [Test]
        public void CompareJointPose_WithEmptyReplayPoses_ShouldReturnZeroSimilarity()
        {
            // Arrange
            var replayPoses = new Dictionary<int, PoseData>();
            // HandVisual은 실제 Unity 오브젝트가 필요하므로 null로 테스트

            // Act
            var result = comparer.CompareJointPose(null, replayPoses, "TestHand");

            // Assert
            Assert.AreEqual(0f, result.similarity);
            Assert.IsFalse(result.passed);
        }
    }

    /// <summary>
    /// HandPoseConfig 테스트
    /// </summary>
    public class HandPoseConfigTests
    {
        [Test]
        public void Default_ShouldReturnValidConfig()
        {
            // Act
            var config = HandPoseConfig.Default();

            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(0.1f, config.playbackInterval);
            Assert.AreEqual(1.0f, config.playbackSpeed);
            Assert.AreEqual(0.05f, config.positionThreshold);
            Assert.AreEqual(15f, config.rotationThreshold);
            Assert.AreEqual(0.7f, config.similarityPercentage);
        }

        [Test]
        public void Clone_ShouldCreateIdenticalCopy()
        {
            // Arrange
            var original = HandPoseConfig.Default();
            original.positionThreshold = 0.123f;
            original.showDebugGizmos = false;

            // Act
            var cloned = original.Clone();

            // Assert
            Assert.AreNotSame(original, cloned);
            Assert.AreEqual(original.positionThreshold, cloned.positionThreshold);
            Assert.AreEqual(original.showDebugGizmos, cloned.showDebugGizmos);
        }

        [Test]
        public void Clone_ModifyingClone_ShouldNotAffectOriginal()
        {
            // Arrange
            var original = HandPoseConfig.Default();
            var cloned = original.Clone();

            // Act
            cloned.positionThreshold = 999f;

            // Assert
            Assert.AreNotEqual(original.positionThreshold, cloned.positionThreshold);
            Assert.AreEqual(0.05f, original.positionThreshold);
        }
    }
}
