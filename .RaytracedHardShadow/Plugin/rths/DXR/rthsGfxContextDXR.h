#pragma once
#ifdef _WIN32
#include "rthsRenderer.h"
#include "rthsTypesDXR.h"
#include "rthsResourceTranslatorDXR.h"
#include "rthsDeformerDXR.h"

#define rthsMaxBounce 2

namespace rths {

class GfxContextDXR
{
public:
    static bool initializeInstance();
    static void finalizeInstance();
    static GfxContextDXR* getInstance();

    bool valid() const;
    bool validateDevice();
    ID3D12Device5Ptr getDevice();

    bool initializeDevice();
    void prepare(RenderDataDXR& rd);
    void setSceneData(RenderDataDXR& rd, SceneData& data);
    void setRenderTarget(RenderDataDXR& rd, TextureData& rt);
    void setMeshes(RenderDataDXR& rd, std::vector<MeshInstanceData*>& instances);
    uint64_t flush(RenderDataDXR& rd);
    void finish(RenderDataDXR& rd);

    void onFrameBegin();
    void onFrameEnd();
    void onMeshDelete(MeshData *mesh);
    void onMeshInstanceDelete(MeshInstanceData *inst);

    ID3D12ResourcePtr createBuffer(uint64_t size, D3D12_RESOURCE_FLAGS flags, D3D12_RESOURCE_STATES state, const D3D12_HEAP_PROPERTIES& heap_props);
    ID3D12ResourcePtr createTexture(int width, int height, DXGI_FORMAT format);

    void addResourceBarrier(ID3D12GraphicsCommandList4Ptr cl, ID3D12ResourcePtr resource, D3D12_RESOURCE_STATES state_before, D3D12_RESOURCE_STATES state_after);
    uint64_t submitCommandList(ID3D12GraphicsCommandList4Ptr cl);
    bool readbackBuffer(ID3D12GraphicsCommandList4Ptr cl, void *dst, ID3D12Resource *src, size_t size);
    bool uploadBuffer(ID3D12GraphicsCommandList4Ptr cl, ID3D12Resource *dst, const void *src, size_t size);
    bool readbackTexture(ID3D12GraphicsCommandList4Ptr cl, void *dst, ID3D12Resource *src, size_t width, size_t height, size_t stride);
    bool uploadTexture(ID3D12GraphicsCommandList4Ptr cl, ID3D12Resource *dst, const void *src, size_t width, size_t height, size_t stride);
    void executeAndWaitCopy(ID3D12GraphicsCommandList4Ptr cl);

private:
    friend std::unique_ptr<GfxContextDXR> std::make_unique<GfxContextDXR>();
    friend struct std::default_delete<GfxContextDXR>;

    GfxContextDXR();
    ~GfxContextDXR();

    IResourceTranslatorPtr m_resource_translator;
    DeformerDXRPtr m_deformer;

    ID3D12Device5Ptr m_device;

    // command list for raytrace
    ID3D12CommandAllocatorPtr m_cmd_allocator_direct;
    ID3D12CommandQueuePtr m_cmd_queue_direct;

    // command list for copy resources
    ID3D12CommandAllocatorPtr m_cmd_allocator_copy;
    ID3D12CommandQueuePtr m_cmd_queue_copy;

    ID3D12FencePtr m_fence;
    FenceEventDXR m_fence_event;

    ID3D12StateObjectPtr m_pipeline_state;
    ID3D12RootSignaturePtr m_global_rootsig;
    ID3D12RootSignaturePtr m_local_rootsig;

    std::map<void*, TextureDataDXRPtr> m_texture_records;
    std::map<void*, BufferDataDXRPtr> m_buffer_records;
    std::map<MeshData*, MeshDataDXRPtr> m_mesh_records;
    std::map<MeshInstanceData*, MeshInstanceDataDXRPtr> m_meshinstance_records;

    FrameBeginCallback m_on_frame_begin;
    FrameEndCallback m_on_frame_end;
    MeshDataCallback m_on_mesh_delete;
    MeshInstanceDataCallback m_on_meshinstance_delete;

    bool m_flushing = false;

#ifdef rthsEnableTimestamp
    TimestampDXRPtr m_timestamp;
#endif // rthsEnableTimestamp
};

} // namespace rths
#endif
