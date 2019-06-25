﻿using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_2019_1_OR_NEWER
using Unity.Collections;
#endif

namespace UTJ.RaytracedHardShadow
{
    public static class Misc
    {
        public static string CString(IntPtr ptr)
        {
            return ptr == IntPtr.Zero ? "" : Marshal.PtrToStringAnsi(ptr);
        }

        public static void SafeDispose<T>(ref T obj) where T : class, IDisposable
        {
            if (obj != null)
            {
                obj.Dispose();
                obj = null;
            }
        }

#if UNITY_2019_1_OR_NEWER
        public static void SafeDispose<T>(ref NativeArray<T> obj) where T : struct
        {
            if (obj.IsCreated)
                obj.Dispose();
        }

        // explicit layout doesn't work with generics...

        [StructLayout(LayoutKind.Explicit)]
        struct NAByte
        {
            [FieldOffset(0)] public NativeArray<byte> nativeArray;
            [FieldOffset(0)] public IntPtr pointer;
        }
        public static IntPtr GetPointer(ref NativeArray<byte> na)
        {
            var union = new NAByte();
            union.nativeArray = na;
            return union.pointer;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct NABoneWeight1
        {
            [FieldOffset(0)] public NativeArray<BoneWeight1> nativeArray;
            [FieldOffset(0)] public IntPtr pointer;
        }
        public static IntPtr GetPointer(ref NativeArray<BoneWeight1> na)
        {
            var union = new NABoneWeight1();
            union.nativeArray = na;
            return union.pointer;
        }
#endif
    }

    public enum rthsRenderFlag
    {
        CullBackFace            = 0x0001,
        IgnoreSelfShadow        = 0x0002,
        KeepSelfDropShadow      = 0x0004,
        GPUSkinning             = 0x0100,
        ClampBlendShapeWights   = 0x0200,
    }

    public enum rthsHitMask
    {
        Rceiver = 0x0001,
        Caster  = 0x0002,
        All = Rceiver | Caster,
    }

    public struct rthsMeshData
    {
        #region internal
        public IntPtr self;
        [DllImport("rths")] static extern IntPtr rthsMeshCreate();
        [DllImport("rths")] static extern void rthsMeshRelease(IntPtr self);
        [DllImport("rths")] static extern void rthsMeshSetGPUBuffers(IntPtr self, IntPtr vb, IntPtr ib, int vertexStride, int vertexCount, int vertexOffset, int indexStride, int indexCount, int indexOffset);
        [DllImport("rths")] static extern void rthsMeshSetSkinBindposes(IntPtr self, Matrix4x4[] bindposes, int num_bindposes);
        [DllImport("rths")] static extern void rthsMeshSetSkinWeights(IntPtr self, IntPtr c, int nc, IntPtr w, int nw);
        [DllImport("rths")] static extern void rthsMeshSetSkinWeights4(IntPtr self, BoneWeight[] w4, int nw4);
        [DllImport("rths")] static extern void rthsMeshSetBlendshapeCount(IntPtr self, int num_bs);
        [DllImport("rths")] static extern void rthsMeshAddBlendshapeFrame(IntPtr self, int bs_index, Vector3[] delta, float weight);

        #endregion

        public static implicit operator bool(rthsMeshData v) { return v.self != IntPtr.Zero; }
        public static bool operator ==(rthsMeshData a, rthsMeshData b) { return a.self == b.self; }
        public static bool operator !=(rthsMeshData a, rthsMeshData b) { return a.self != b.self; }

        public static rthsMeshData Create()
        {
            return new rthsMeshData { self = rthsMeshCreate() };
        }

        public void Release()
        {
            rthsMeshRelease(self);
            self = IntPtr.Zero;
        }

        public void SetGPUBuffers(IntPtr vb, IntPtr ib, int vertexStride, int vertexCount, int vertexOffset, int indexStride, int indexCount, int indexOffset)
        {
            rthsMeshSetGPUBuffers(self, vb, ib, vertexStride, vertexCount, vertexOffset, indexStride, indexCount, indexOffset);
        }

        public void SetBindpose(Matrix4x4[] bindposes)
        {
            rthsMeshSetSkinBindposes(self, bindposes, bindposes.Length);
        }
#if UNITY_2019_1_OR_NEWER
        public void SetSkinWeights(NativeArray<byte> counts, NativeArray<BoneWeight1> weights)
        {
            rthsMeshSetSkinWeights(self, Misc.GetPointer(ref counts), counts.Length, Misc.GetPointer(ref weights), weights.Length);
        }
#endif
        public void SetSkinWeights(BoneWeight[] w4)
        {
            rthsMeshSetSkinWeights4(self, w4, w4.Length);
        }

        public void SetBlendshapeCount(int v)
        {
            rthsMeshSetBlendshapeCount(self, v);
        }

        public void AddBlendshapeFrame(int index, Vector3[] delta, float weight)
        {
            rthsMeshAddBlendshapeFrame(self, index, delta, weight);
        }


        public void SetGPUBuffers(Mesh mesh)
        {
            int indexStride = mesh.indexFormat == UnityEngine.Rendering.IndexFormat.UInt16 ? 2 : 4;
            int indexCountMerged = 0;
            int prevIndexEnd = 0;

            // merge continuous triangle submeshes into a single one
            int subMeshCount = mesh.subMeshCount;
            for (int smi = 0; smi < subMeshCount; ++smi)
            {
                if (mesh.GetTopology(smi) != MeshTopology.Triangles)
                    break;
                int start = (int)mesh.GetIndexStart(smi);
                if (start != prevIndexEnd)
                    break;
                int indexCount = (int)mesh.GetIndexCount(smi);
                indexCountMerged += (int)mesh.GetIndexCount(smi);
                prevIndexEnd = start + indexCount;
            }

            SetGPUBuffers(
                mesh.GetNativeVertexBufferPtr(0), mesh.GetNativeIndexBufferPtr(),
                0, mesh.vertexCount, 0, indexStride, indexCountMerged, 0);
        }
    }

    public struct rthsMeshInstanceData
    {
        #region internal
        public IntPtr self;
        [DllImport("rths")] static extern IntPtr rthsMeshInstanceCreate(rthsMeshData mesh);
        [DllImport("rths")] static extern void rthsMeshInstanceRelease(IntPtr self);
        [DllImport("rths")] static extern void rthsMeshInstanceSetTransform(IntPtr self, Matrix4x4 transform);
        [DllImport("rths")] static extern void rthsMeshInstanceSetBones(IntPtr self, Matrix4x4[] bones, int num_bones);
        [DllImport("rths")] static extern void rthsMeshInstanceSetBlendshapeWeights(IntPtr self, float[] bsw, int num_bsw);

        #endregion

        public static implicit operator bool(rthsMeshInstanceData v) { return v.self != IntPtr.Zero; }
        public static bool operator ==(rthsMeshInstanceData a, rthsMeshInstanceData b) { return a.self == b.self; }
        public static bool operator !=(rthsMeshInstanceData a, rthsMeshInstanceData b) { return a.self != b.self; }

        public static rthsMeshInstanceData Create(rthsMeshData mesh)
        {
            return new rthsMeshInstanceData { self = rthsMeshInstanceCreate(mesh) };
        }

        public void Release()
        {
            rthsMeshInstanceRelease(self);
            self = IntPtr.Zero;
        }

        public void SetTransform(Matrix4x4 transform)
        {
            rthsMeshInstanceSetTransform(self, transform);
        }

        public void SetBones(Matrix4x4[] bones)
        {
            if (bones == null)
                rthsMeshInstanceSetBones(self, null, 0);
            else
                rthsMeshInstanceSetBones(self, bones, bones.Length);
        }
        public void SetBones(Transform[] bones)
        {
            if (bones == null)
            {
                rthsMeshInstanceSetBones(self, null, 0);
            }
            else
            {
                int n = bones.Length;
                var transforms = new Matrix4x4[n];
                for (int bi = 0; bi < n; ++bi)
                {
                    var bone = bones[bi];
                    transforms[bi] = bone ? bone.localToWorldMatrix : Matrix4x4.identity;
                }
                SetBones(transforms);
            }
        }
        public void SetBones(SkinnedMeshRenderer smr)
        {
            if (smr.rootBone != null)
                SetBones(smr.bones);
        }

        public void SetBlendshapeWeights(float[] bsw)
        {
            if (bsw == null)
                rthsMeshInstanceSetBlendshapeWeights(self, null, 0);
            else
                rthsMeshInstanceSetBlendshapeWeights(self, bsw, bsw.Length);
        }
        public void SetBlendshapeWeights(SkinnedMeshRenderer smr)
        {
            Mesh mesh = null;
            if (smr != null)
                mesh = smr.sharedMesh;

            if (mesh != null)
            {
                int nbs = mesh.blendShapeCount;
                if (nbs > 0)
                {
                    var weights = new float[nbs];
                    for (int bsi = 0; bsi < nbs; ++bsi)
                        weights[bsi] = smr.GetBlendShapeWeight(bsi);
                    SetBlendshapeWeights(weights);
                }
            }
            else
            {
                rthsMeshInstanceSetBlendshapeWeights(self, null, 0);
            }
        }
    }



    public struct rthsRenderer
    {
        #region internal
        public IntPtr self;
        [DllImport("rths")] static extern IntPtr rthsGetErrorLog();
        [DllImport("rths")] static extern IntPtr rthsCreateRenderer();
        [DllImport("rths")] static extern void rthsReleaseRenderer(IntPtr self);

        [DllImport("rths")] static extern void rthsBeginScene(IntPtr self);
        [DllImport("rths")] static extern void rthsEndScene(IntPtr self);
        [DllImport("rths")] static extern void rthsSetRenderFlags(IntPtr self, int flags);
        [DllImport("rths")] static extern void rthsSetShadowRayOffset(IntPtr self, float v);
        [DllImport("rths")] static extern void rthsSetSelfShadowThreshold(IntPtr self, float v);
        [DllImport("rths")] static extern void rthsSetRenderTarget(IntPtr self, IntPtr rt);
        [DllImport("rths")] static extern void rthsSetCamera(IntPtr self, Matrix4x4 trans, Matrix4x4 view, Matrix4x4 proj, float near, float far, float fov);
        [DllImport("rths")] static extern void rthsAddDirectionalLight(IntPtr self, Matrix4x4 trans);
        [DllImport("rths")] static extern void rthsAddSpotLight(IntPtr self, Matrix4x4 trans, float range, float spotAngle);
        [DllImport("rths")] static extern void rthsAddPointLight(IntPtr self, Matrix4x4 trans, float range);
        [DllImport("rths")] static extern void rthsAddReversePointLight(IntPtr self, Matrix4x4 trans, float range);
        [DllImport("rths")] static extern void rthsAddGeometry(IntPtr self, rthsMeshInstanceData mesh, byte hitMask);

        [DllImport("rths")] static extern IntPtr rthsGetRenderAll();
        #endregion

        public static string errorLog
        {
            get { return Misc.CString(rthsGetErrorLog()); }
        }

        public static implicit operator bool(rthsRenderer v) { return v.self != IntPtr.Zero; }
        public static rthsRenderer Create()
        {
            // rthsCreateRenderer() will return null if DXR is not supported
            return new rthsRenderer { self = rthsCreateRenderer() };
        }

        public void Release()
        {
            rthsReleaseRenderer(self);
            self = IntPtr.Zero;
        }

        public void SetRenderTarget(RenderTexture rt)
        {
            rthsSetRenderTarget(self, rt.GetNativeTexturePtr());
        }

        public void BeginScene()
        {
            rthsBeginScene(self);
        }

        public void EndScene()
        {
            rthsEndScene(self);
        }

        public void SetRaytraceFlags(int flags)
        {
            rthsSetRenderFlags(self, flags);
        }
        public void SetShadowRayOffset(float v)
        {
            rthsSetShadowRayOffset(self, v);
        }
        public void SetSelfShadowThreshold(float v)
        {
            rthsSetSelfShadowThreshold(self, v);
        }

        public void SetCamera(Camera cam)
        {
            rthsSetCamera(self, cam.transform.localToWorldMatrix, cam.worldToCameraMatrix, cam.projectionMatrix, cam.nearClipPlane, cam.farClipPlane, cam.fieldOfView);
        }

        public bool AddLight(Light light)
        {
            switch (light.type)
            {
                case LightType.Directional:
                    rthsAddDirectionalLight(self, light.transform.localToWorldMatrix);
                    return true;
                case LightType.Spot:
                    rthsAddSpotLight(self, light.transform.localToWorldMatrix, light.range, light.spotAngle);
                    return true;
                case LightType.Point:
                    rthsAddPointLight(self, light.transform.localToWorldMatrix, light.range);
                    return true;
                default:
                    Debug.LogWarning("rthsShadowRenderer: " + light.type + " is not supported");
                    return false;
            }
        }
        public bool AddLight(ShadowCasterLight light)
        {
            switch (light.lightType)
            {
                case ShadowCasterLightType.Directional:
                    rthsAddDirectionalLight(self, light.transform.localToWorldMatrix);
                    return true;
                case ShadowCasterLightType.Spot:
                    rthsAddSpotLight(self, light.transform.localToWorldMatrix, light.range, light.spotAngle);
                    return true;
                case ShadowCasterLightType.Point:
                    rthsAddPointLight(self, light.transform.localToWorldMatrix, light.range);
                    return true;
                case ShadowCasterLightType.ReversePoint:
                    rthsAddReversePointLight(self, light.transform.localToWorldMatrix, light.range);
                    return true;
                default:
                    Debug.LogWarning("rthsShadowRenderer: " + light.lightType + " is not supported");
                    return false;
            }
        }

        public void AddGeometry(rthsMeshInstanceData mesh, byte hitMask = 0xff)
        {
            rthsAddGeometry(self, mesh, hitMask);
        }

        public static void IssueRender()
        {
            GL.IssuePluginEvent(rthsGetRenderAll(), 0);
        }
        public static void IssueRender(CommandBuffer cb)
        {
            cb.IssuePluginEvent(rthsGetRenderAll(), 0);
        }
    }
}
