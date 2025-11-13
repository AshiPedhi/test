using ChunaVR.Core;
using UnityEngine;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Input;

namespace ChunaVR.PoseData
{
    /// <summary>
    /// 손 포즈 비교 및 평가 전용 클래스
    /// HandPosePlayer에서 분리
    /// </summary>
    public class HandPoseComparer
    {
        [System.Serializable]
        public struct ComparisonResult
        {
            public float similarity;
            public bool passed;
            public float positionError;
            public float rotationError;
            public bool positionPassed;
            public bool rotationPassed;
        }

        [System.Serializable]
        public struct DualHandResult
        {
            public ComparisonResult leftHand;
            public ComparisonResult rightHand;
            public bool overallPassed;
            public float averageSimilarity;
        }

        private HandPoseConfig config;
        private Transform referencePoint;

        public HandPoseComparer(HandPoseConfig config, Transform referencePoint = null)
        {
            this.config = config;
            this.referencePoint = referencePoint;
        }

        /// <summary>
        /// 조인트 포즈 비교
        /// </summary>
        public ComparisonResult CompareJointPose(
            HandVisual playerHand,
            Dictionary<int, PoseData> replayPoses,
            string handName = "")
        {
            ComparisonResult result = new ComparisonResult();

            if (playerHand == null || playerHand.Hand == null || !playerHand.Hand.IsTrackedDataValid)
            {
                return result;
            }

            int similarJointCount = 0;
            int totalJointCount = 0;

            foreach (HandJointId jointId in config.keyJoints)
            {
                int jointIndex = (int)jointId;

                if (!replayPoses.ContainsKey(jointIndex))
                    continue;

                totalJointCount++;

                if (jointIndex >= playerHand.Joints.Count || playerHand.Joints[jointIndex] == null)
                    continue;

                Transform playerJoint = playerHand.Joints[jointIndex];
                PoseData replayPose = replayPoses[jointIndex];

                float positionDistance = Vector3.Distance(playerJoint.localPosition, replayPose.position);
                float rotationAngle = Quaternion.Angle(playerJoint.localRotation, replayPose.rotation);

                if (positionDistance <= config.positionThreshold && rotationAngle <= config.rotationThreshold)
                {
                    similarJointCount++;
                }
            }

            if (totalJointCount == 0)
            {
                result.similarity = 0f;
                result.passed = false;
                return result;
            }

            result.similarity = (float)similarJointCount / totalJointCount;
            result.passed = result.similarity >= config.similarityPercentage;

            return result;
        }

        /// <summary>
        /// 손 전체 위치/회전 비교
        /// </summary>
        public ComparisonResult CompareHandWorldTransform(
            HandVisual playerHand,
            Vector3 targetWorldPosition,
            Quaternion targetWorldRotation,
            string handName = "")
        {
            ComparisonResult result = new ComparisonResult();

            if (playerHand == null || playerHand.Hand == null || !playerHand.Hand.IsTrackedDataValid)
                return result;

            Transform wrist = playerHand.Joints[(int)HandJointId.HandWristRoot];
            if (wrist == null)
                return result;

            // 기준점 적용
            Vector3 targetPos = targetWorldPosition;
            if (referencePoint != null)
            {
                targetPos = referencePoint.position + targetWorldPosition;
            }

            Vector3 playerPos = wrist.position;

            // 위치 오차 계산
            result.positionError = Vector3.Distance(playerPos, targetPos);
            result.positionPassed = result.positionError <= config.handPositionThreshold;

            // 회전 오차 계산
            if (config.compareHandRotation)
            {
                result.rotationError = Quaternion.Angle(wrist.rotation, targetWorldRotation);
                result.rotationPassed = result.rotationError <= config.handRotationThreshold;
            }
            else
            {
                result.rotationPassed = true;
            }

            result.passed = result.positionPassed && result.rotationPassed;

            return result;
        }

        /// <summary>
        /// 양손 통합 비교
        /// </summary>
        public DualHandResult CompareBothHands(
            HandVisual leftPlayerHand,
            HandVisual rightPlayerHand,
            Dictionary<int, PoseData> leftReplayPoses,
            Dictionary<int, PoseData> rightReplayPoses,
            Vector3 leftWorldPos,
            Quaternion leftWorldRot,
            Vector3 rightWorldPos,
            Quaternion rightWorldRot)
        {
            DualHandResult result = new DualHandResult();

            // 왼손 조인트 비교
            if (config.compareLeftHand && leftPlayerHand != null)
            {
                result.leftHand = CompareJointPose(leftPlayerHand, leftReplayPoses, "왼손");

                // 왼손 위치 비교
                if (config.compareHandPosition)
                {
                    var posResult = CompareHandWorldTransform(leftPlayerHand, leftWorldPos, leftWorldRot, "왼손");
                    result.leftHand.positionError = posResult.positionError;
                    result.leftHand.rotationError = posResult.rotationError;
                    result.leftHand.positionPassed = posResult.positionPassed;
                    result.leftHand.rotationPassed = posResult.rotationPassed;
                    result.leftHand.passed = result.leftHand.passed && posResult.passed;
                }
            }
            else
            {
                result.leftHand.passed = true;
                result.leftHand.similarity = 1f;
            }

            // 오른손 조인트 비교
            if (config.compareRightHand && rightPlayerHand != null)
            {
                result.rightHand = CompareJointPose(rightPlayerHand, rightReplayPoses, "오른손");

                // 오른손 위치 비교
                if (config.compareHandPosition)
                {
                    var posResult = CompareHandWorldTransform(rightPlayerHand, rightWorldPos, rightWorldRot, "오른손");
                    result.rightHand.positionError = posResult.positionError;
                    result.rightHand.rotationError = posResult.rotationError;
                    result.rightHand.positionPassed = posResult.positionPassed;
                    result.rightHand.rotationPassed = posResult.rotationPassed;
                    result.rightHand.passed = result.rightHand.passed && posResult.passed;
                }
            }
            else
            {
                result.rightHand.passed = true;
                result.rightHand.similarity = 1f;
            }

            // 전체 결과
            result.overallPassed = result.leftHand.passed && result.rightHand.passed;
            result.averageSimilarity = (result.leftHand.similarity + result.rightHand.similarity) / 2f;

            return result;
        }

        /// <summary>
        /// 설정 업데이트
        /// </summary>
        public void UpdateConfig(HandPoseConfig newConfig)
        {
            config = newConfig;
        }

        /// <summary>
        /// 기준점 설정
        /// </summary>
        public void SetReferencePoint(Transform reference)
        {
            referencePoint = reference;
        }

        /// <summary>
        /// 현재 설정 가져오기
        /// </summary>
        public HandPoseConfig GetConfig()
        {
            return config;
        }
    }

    /// <summary>
    /// 포즈 데이터 구조
    /// </summary>
    [System.Serializable]
    public class PoseData
    {
        public Vector3 position;
        public Quaternion rotation;
    }
}
