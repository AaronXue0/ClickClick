using UnityEngine;

namespace ClickClick
{
    public class Tester : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SceneTransition.Instance.TransitionToScene("AnimationTest");
            }
        }
    }
}