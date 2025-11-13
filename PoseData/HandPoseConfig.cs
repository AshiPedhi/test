using ChunaVR.Core;
using UnityEngine;
using System.Collections.Generic;
using Oculus.Interaction.Input;

namespace ChunaVR.PoseData
{
    /// <summary>
    /// HandPose 시스템 설정 관리
    /// HandPosePlayer에서 분리된 설정 전용 클래스
    /// </summary>
    [System.Serializable]
    public class HandPoseConfig
    {
        [Header("재생 설정")]
        [Tooltip("프레임 간 재생 간격 (초)")]
        public float playbackInterval = 0.1f;

        [Tooltip("재생 속도 배율")]
        public float playbackSpeed = 1.0f;

        [Header("비교 임계값")]
        [Tooltip("위치 비교 임계값 (미터)")]
        public float positionThreshold = 0.05f;

        [Tooltip("회전 비교 임계값 (도)")]
        public float rotationThreshold = 15f;

        [Tooltip("유사도 통과 기준 (0~1)")]
        public float similarityPercentage = 0.7f;

        [Header("손 전체 위치 비교")]
        [Tooltip("손 전체 위치 비교 활성화")]
        public bool compareHandPosition = true;

        [Tooltip("손 위치 임계값 (미터)")]
        public float handPositionThreshold = 0.1f;

        [Tooltip("손 회전 비교 활성화")]
        public bool compareHandRotation = true;

        [Tooltip("손 회전 임계값 (도)")]
        public float handRotationThreshold = 20f;

        [Header("리플레이 손 표시")]
        [Tooltip("리플레이 손 표시 여부")]
        public bool showReplayHands = true;

        [Tooltip("리플레이 손 투명도")]
        [Range(0f, 1f)]
        public float replayHandAlpha = 0.5f;

        [Tooltip("리플레이 손 색상")]
        public Color replayHandColor = new Color(0.3f, 0.5f, 1f, 0.5f);

        [Header("재생 제어")]
        [Tooltip("왼손 재생")]
        public bool playLeftHand = true;

        [Tooltip("오른손 재생")]
        public bool playRightHand = true;

        [Tooltip("왼손 비교")]
        public bool compareLeftHand = true;

        [Tooltip("오른손 비교")]
        public bool compareRightHand = true;

        [Tooltip("자동 진행 활성화")]
        public bool enableAutoProgress = true;

        [Header("주요 조인트")]
        [Tooltip("비교할 주요 조인트 목록")]
        public List<HandJointId> keyJoints = new List<HandJointId>()
        {
            HandJointId.HandWristRoot,
            HandJointId.HandThumb3,
            HandJointId.HandIndex3,
            HandJointId.HandMiddle3,
            HandJointId.HandRing3,
            HandJointId.HandPinky3
        };

        [Header("디버그 설정")]
        [Tooltip("디버그 Gizmo 표시")]
        public bool showDebugGizmos = true;

        [Tooltip("위치 연결선 표시")]
        public bool showPositionLines = true;

        [Tooltip("Gizmo 구체 크기")]
        public float gizmoSphereSize = 0.02f;

        /// <summary>
        /// 기본 설정 생성
        /// </summary>
        public static HandPoseConfig Default()
        {
            return new HandPoseConfig
            {
                playbackInterval = 0.1f,
                playbackSpeed = 1.0f,
                positionThreshold = 0.05f,
                rotationThreshold = 15f,
                similarityPercentage = 0.7f,
                compareHandPosition = true,
                handPositionThreshold = 0.1f,
                compareHandRotation = true,
                handRotationThreshold = 20f,
                showReplayHands = true,
                replayHandAlpha = 0.5f,
                replayHandColor = new Color(0.3f, 0.5f, 1f, 0.5f),
                playLeftHand = true,
                playRightHand = true,
                compareLeftHand = true,
                compareRightHand = true,
                enableAutoProgress = true,
                showDebugGizmos = true,
                showPositionLines = true,
                gizmoSphereSize = 0.02f
            };
        }

        /// <summary>
        /// 설정 복사
        /// </summary>
        public HandPoseConfig Clone()
        {
            return new HandPoseConfig
            {
                playbackInterval = this.playbackInterval,
                playbackSpeed = this.playbackSpeed,
                positionThreshold = this.positionThreshold,
                rotationThreshold = this.rotationThreshold,
                similarityPercentage = this.similarityPercentage,
                compareHandPosition = this.compareHandPosition,
                handPositionThreshold = this.handPositionThreshold,
                compareHandRotation = this.compareHandRotation,
                handRotationThreshold = this.handRotationThreshold,
                showReplayHands = this.showReplayHands,
                replayHandAlpha = this.replayHandAlpha,
                replayHandColor = this.replayHandColor,
                playLeftHand = this.playLeftHand,
                playRightHand = this.playRightHand,
                compareLeftHand = this.compareLeftHand,
                compareRightHand = this.compareRightHand,
                enableAutoProgress = this.enableAutoProgress,
                keyJoints = new List<HandJointId>(this.keyJoints),
                showDebugGizmos = this.showDebugGizmos,
                showPositionLines = this.showPositionLines,
                gizmoSphereSize = this.gizmoSphereSize
            };
        }
    }
}
