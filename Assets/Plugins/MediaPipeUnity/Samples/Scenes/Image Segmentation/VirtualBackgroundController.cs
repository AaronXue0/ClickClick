using UnityEngine;
using Mediapipe.Tasks.Vision.ImageSegmenter;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using Mediapipe.Unity.Sample.ImageSegmentation;
using Mediapipe.Unity;
using Mediapipe;
using Mediapipe.Unity.Sample;

public class VirtualBackgroundController : MonoBehaviour
{
    [SerializeField] private RawImage _cameraFeedDisplay;
    [SerializeField] private Texture2D _backgroundTexture;
    [SerializeField, Range(0f, 1f)] private float _maskThreshold = 0.9f;
    [SerializeField] private UnityEngine.Color _backgroundColor = UnityEngine.Color.green;

    private Material _virtualBgMaterial;
    private Renderer _renderer;
    private GraphicsBuffer _maskBuffer;
    private float[] _maskArray;
    private int _width;
    private int _height;
    private bool _isInitialized = false;

    // External references
    private ImageSegmenterRunner _imageSegmenterRunner;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _imageSegmenterRunner = FindObjectOfType<ImageSegmenterRunner>();

        if (_imageSegmenterRunner == null)
        {
            Debug.LogError("Could not find ImageSegmenterRunner in the scene.");
        }
    }

    private void Start()
    {
        _virtualBgMaterial = new Material(Shader.Find("Custom/VirtualBackground"));
        _renderer.material = _virtualBgMaterial;

        // Wait for a frame to ensure ImageSource is initialized
        StartCoroutine(InitializeAfterImageSourceReady());
    }

    private System.Collections.IEnumerator InitializeAfterImageSourceReady()
    {
        // Wait until the image source is ready
        var imageSource = ImageSourceProvider.ImageSource;
        while (imageSource == null || !imageSource.isPrepared)
        {
            yield return null;
        }

        Initialize(imageSource.textureWidth, imageSource.textureHeight);

        // Register the callback to process segmentation results
        _imageSegmenterRunner.OnSegmentationResultAvailable += ProcessMask;
    }

    private void OnDestroy()
    {
        if (_imageSegmenterRunner != null)
        {
            _imageSegmenterRunner.OnSegmentationResultAvailable -= ProcessMask;
        }

        if (_maskBuffer != null)
        {
            _maskBuffer.Release();
            _maskBuffer = null;
        }

        _maskArray = null;
    }

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;

        // Initialize mask buffer and array
        if (_maskBuffer != null)
        {
            _maskBuffer.Release();
        }

        var stride = Marshal.SizeOf(typeof(float));
        _maskBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, width * height, stride);
        _maskArray = new float[width * height];

        // Setup material properties
        _virtualBgMaterial.SetTexture("_CameraFeedTex", _cameraFeedDisplay.texture);
        _virtualBgMaterial.SetTexture("_BackgroundTex", _backgroundTexture);
        _virtualBgMaterial.SetColor("_BackgroundColor", _backgroundColor);
        _virtualBgMaterial.SetFloat("_Threshold", _maskThreshold);
        _virtualBgMaterial.SetBuffer("_MaskBuffer", _maskBuffer);
        _virtualBgMaterial.SetInt("_Width", width);
        _virtualBgMaterial.SetInt("_Height", height);

        _isInitialized = true;

        Debug.Log($"VirtualBackgroundController initialized with dimensions: {width}x{height}");
    }

    public void SetBackgroundTexture(Texture2D texture)
    {
        _backgroundTexture = texture;
        if (_virtualBgMaterial != null)
        {
            _virtualBgMaterial.SetTexture("_BackgroundTex", _backgroundTexture);
        }
    }

    public void SetBackgroundColor(UnityEngine.Color color)
    {
        _backgroundColor = color;
        if (_virtualBgMaterial != null)
        {
            _virtualBgMaterial.SetColor("_BackgroundColor", _backgroundColor);
        }
    }

    public void SetThreshold(float threshold)
    {
        _maskThreshold = threshold;
        if (_virtualBgMaterial != null)
        {
            _virtualBgMaterial.SetFloat("_Threshold", _maskThreshold);
        }
    }

    // This method will be called when new segmentation results are available
    public void ProcessMask(ImageSegmenterResult result)
    {
        if (!_isInitialized || result.confidenceMasks == null || result.confidenceMasks.Count == 0)
        {
            return;
        }

        // Use the first mask for person segmentation
        // (usually index 0 for person/background segmentation)
        var mask = result.confidenceMasks[0];

        // Process the mask data
        if (mask != null)
        {
            // The mask values are already between 0-1, with higher values 
            // corresponding to higher confidence that the pixel belongs to the person
            for (int i = 0; i < _maskArray.Length; i++)
            {
                _maskArray[i] = 0; // Clear the array first
            }

            // Get the raw mask data and copy it to our array
            bool success = false;

            try
            {
                // Read the mask data directly using TryReadChannelNormalized
                success = mask.TryReadChannelNormalized(0, _maskArray);

                if (!success)
                {
                    Debug.LogWarning("Failed to read mask data. Using fallback method.");

                    // Simple fallback method - use a blank mask if reading fails
                    // In a real implementation, you might try more sophisticated approaches
                    for (int i = 0; i < _maskArray.Length; i++)
                    {
                        _maskArray[i] = 0; // Default to background
                    }
                    success = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error processing mask: {e.Message}");
                return;
            }

            if (success)
            {
                // Update the mask buffer with the new values
                _maskBuffer.SetData(_maskArray);
            }
        }
    }
}