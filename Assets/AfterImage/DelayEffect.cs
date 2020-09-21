using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(Camera))]

// https://docs.unity3d.com/Manual/GraphicsCommandBuffers.html
public class DelayEffect : MonoBehaviour
{
  [SerializeField] private AROcclusionManager _occlusionManager;
  [SerializeField] private Shader _shader;

  private const int FRAME_COUNT = 10;
  private const int INTERVAL = 4;


  private readonly List<DelayImage> _delayImages = new List<DelayImage>();
  private readonly List<RenderTexture> _cameraFeedBuffers = new List<RenderTexture>();
  private readonly List<RenderTexture> _stencilBuffers = new List<RenderTexture>();

  private Camera _camera;
  private CommandBuffer _commandBuffer;

  private void Awake()
  {
    _camera = GetComponent<Camera>();

    // Create instances 
    for (int i = 0; i < FRAME_COUNT; i++)
    {
      _delayImages.Add(new DelayImage(_camera, new Material(_shader)));
    }

    var resolution = (960, 720);

    // Create buffer of RenderTextures to copy CameraFeed and HumanStencilTexture of each frames
    for (int i = 0; i < (FRAME_COUNT - 1) * INTERVAL + 1; i++)
    {
      _cameraFeedBuffers.Add(new RenderTexture(_camera.pixelWidth, _camera.pixelHeight, 0));
      // For width and height use predefined options. (Best, Fastest, Medium)
      _stencilBuffers.Add(new RenderTexture(resolution.Item1, resolution.Item2, 0));
    }

    // Create CommandBuffer that copies latest CameraFeed to last RenderTexture in buffer.
    _commandBuffer = new CommandBuffer();
    _commandBuffer.Blit(null, _cameraFeedBuffers.Last());
    _camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, _commandBuffer);
  }

  private void Update()
  {
    // Move one to the right. 
    for (int i = 0; i < _cameraFeedBuffers.Count - 1; i++)
    {
      Graphics.Blit(_cameraFeedBuffers[i + 1], _cameraFeedBuffers[i]);
    }
  }

  private void OnRenderImage(RenderTexture src, RenderTexture dest)
  {
    var humanStencil = _occlusionManager.humanStencilTexture;
    if (humanStencil)
    {
      // When orientation changes
      if (_cameraFeedBuffers.Last().width != _camera.pixelWidth)
      {
        RestartCameraFeedBuffers();
      }

      // Update stencil buffer 
      for (int i = 0; i < _stencilBuffers.Count - 1; i++)
      {
        Graphics.Blit(_stencilBuffers[i + 1], _stencilBuffers[i]);
      }

      Graphics.Blit(humanStencil, _stencilBuffers.Last());

      // Update HumanStencilTexture of material property in every frame
      for (int i = 0; i < _delayImages.Count; i++)
      {
        _delayImages[i].SetMaterialProperty(_stencilBuffers[i * INTERVAL]);
      }
    }

    Graphics.Blit(src, dest);
  }

  private void OnGUI()
  {
    if (Event.current.type.Equals(EventType.Repaint))
    {
      for (int i = 0; i < _delayImages.Count; i++)
      {
        // Draw delay image
        _delayImages[i].Draw(_cameraFeedBuffers[i * INTERVAL]);
      }
    }
  }

  private void RestartCameraFeedBuffers()
  {
    // release buffers
    _commandBuffer.Clear();
    _camera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, _commandBuffer);
    int bufferCount = _cameraFeedBuffers.Count;
    foreach (var cameraFeed in _cameraFeedBuffers)
    {
      cameraFeed.Release();
    }

    _cameraFeedBuffers.Clear();

    // recreate buffers
    for (int i = 0; i < bufferCount; i++)
    {
      _cameraFeedBuffers.Add(new RenderTexture(_camera.pixelWidth, _camera.pixelHeight, 0));
    }

    _commandBuffer.Blit(null, _cameraFeedBuffers.Last());
    _camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, _commandBuffer);
  }
}
