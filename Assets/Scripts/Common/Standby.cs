using UnityEngine;

namespace ClickClick
{
    public class Standby : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                SceneTransition.Instance.TransitionToScene("Menu");
            }
        }
    }
}