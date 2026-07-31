// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Mathematics;
using UnityEngine;

namespace Unity.Cloud.Gltfast.Tests
{
    [RequireComponent(typeof(Camera))]
    public class CameraControl : MonoBehaviour
    {
        [SerializeField]
        OpenGltfDialog gltfDialog;

        Camera m_Camera;

        [SerializeField]
        float distance = 5f;

        [SerializeField]
        float yaw;

        [SerializeField]
        float pitch;

        float3 m_Center;

        void Start()
        {
            m_Camera = GetComponent<Camera>();
            gltfDialog.BoundsUpdated += GltfDialogOnBoundsUpdated;
        }

        void Update()
        {
            var forward = new float3(
                math.cos(pitch) * math.sin(yaw),
                math.sin(pitch),
                math.cos(pitch) * math.cos(yaw)
            );
            transform.position = m_Center + forward * -distance;
            transform.forward = forward;
        }

        void GltfDialogOnBoundsUpdated(Bounds bounds)
        {
            var distanceVertical = bounds.extents.y / math.tan(m_Camera.fieldOfView / 2 * Mathf.Deg2Rad);
            var horizontalDiagonal = math.length(new float2(bounds.extents.x, bounds.extents.z));
            var fieldOfViewHorizontal = 2 * math.degrees(math.atan(math.tan(m_Camera.fieldOfView / 2 * Mathf.Deg2Rad) * m_Camera.aspect));
            var distanceHorizontal = horizontalDiagonal / math.tan(fieldOfViewHorizontal / 2 * Mathf.Deg2Rad);
            distance = math.max(distanceVertical, distanceHorizontal) * 1.5f;
            m_Center = bounds.center;
        }
    }
}
