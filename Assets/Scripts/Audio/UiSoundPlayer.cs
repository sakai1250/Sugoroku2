using UnityEngine;
using UnityEngine.UI;

namespace Sugoroku.Audio
{
    /// <summary>
    /// Button に付けると Kenney Interface Sounds のクリック音を自動再生する。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UiSoundPlayer : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(
                () => GameAudioController.Instance?.PlayButtonClick());
        }
    }
}
