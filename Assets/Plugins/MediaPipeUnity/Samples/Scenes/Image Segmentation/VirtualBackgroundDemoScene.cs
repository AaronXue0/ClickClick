// using UnityEngine;
// using UnityEngine.UI;
// using Mediapipe.Unity.Sample.ImageSegmentation;
// using Mediapipe.Unity.Sample;

// public class VirtualBackgroundDemoScene : MonoBehaviour
// {
//     [SerializeField] private ImageSegmenterRunner _imageSegmenterRunner;
//     [SerializeField] private VirtualBackgroundController _virtualBackgroundController;
//     [SerializeField] private RawImage _cameraFeedDisplay;
//     [SerializeField] private GameObject _virtualBackgroundPlane;

//     [Header("Background Options")]
//     [SerializeField] private Texture2D[] _backgroundTextures;
//     [SerializeField] private Color[] _backgroundColor;
//     [SerializeField, Range(0f, 1f)] private float _defaultThreshold = 0.9f;

//     private int _currentBackgroundIndex = 0;

//     private void Start()
//     {
//         // Create a plane for displaying virtual background if not set
//         if (_virtualBackgroundPlane == null)
//         {
//             _virtualBackgroundPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
//             _virtualBackgroundPlane.transform.position = new Vector3(0, 0, 1);
//             _virtualBackgroundPlane.transform.localScale = new Vector3(1.6f, 0.9f, 1);

//             // Add VirtualBackgroundController if not already present
//             if (_virtualBackgroundController == null)
//             {
//                 _virtualBackgroundController = _virtualBackgroundPlane.AddComponent<VirtualBackgroundController>();
//             }
//         }

//         // Initialize with first background if available
//         if (_backgroundTextures != null && _backgroundTextures.Length > 0)
//         {
//             _virtualBackgroundController.SetBackgroundTexture(_backgroundTextures[0]);
//         }

//         // Set camera feed display
//         _virtualBackgroundController.SetCameraFeedDisplay(_cameraFeedDisplay);

//         // Set initial threshold
//         _virtualBackgroundController.SetThreshold(_defaultThreshold);

//         // Make sure ImageSegmenterRunner is set
//         if (_imageSegmenterRunner == null)
//         {
//             _imageSegmenterRunner = FindObjectOfType<ImageSegmenterRunner>();

//             if (_imageSegmenterRunner == null)
//             {
//                 Debug.LogError("ImageSegmenterRunner not found. Make sure it's in the scene.");
//             }
//         }
//     }

//     // UI methods for controlling the virtual background

//     public void NextBackground()
//     {
//         if (_backgroundTextures == null || _backgroundTextures.Length == 0) return;

//         _currentBackgroundIndex = (_currentBackgroundIndex + 1) % _backgroundTextures.Length;
//         _virtualBackgroundController.SetBackgroundTexture(_backgroundTextures[_currentBackgroundIndex]);
//     }

//     public void PreviousBackground()
//     {
//         if (_backgroundTextures == null || _backgroundTextures.Length == 0) return;

//         _currentBackgroundIndex--;
//         if (_currentBackgroundIndex < 0)
//             _currentBackgroundIndex = _backgroundTextures.Length - 1;

//         _virtualBackgroundController.SetBackgroundTexture(_backgroundTextures[_currentBackgroundIndex]);
//     }

//     public void SetThreshold(float value)
//     {
//         _virtualBackgroundController.SetThreshold(value);
//     }

//     public void UseColorBackground(int colorIndex)
//     {
//         if (_backgroundColor == null || colorIndex >= _backgroundColor.Length) return;

//         _virtualBackgroundController.SetBackgroundColor(_backgroundColor[colorIndex]);
//     }
// }