using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Inspection
{
    /// <summary>屏幕顶部的短暂提示：成功绿色、失败红色，2 秒后淡出。</summary>
    public class ToastUI : MonoBehaviour
    {
        public static readonly Color Success = new Color(0.25f, 0.85f, 0.45f);
        public static readonly Color Error = new Color(1f, 0.38f, 0.38f);
        public static readonly Color Info = new Color(0.9f, 0.9f, 0.9f);

        public Text label;
        public CanvasGroup group;

        Coroutine routine;

        public void Show(string message, Color color)
        {
            if (label == null || group == null) return;

            label.text = message;
            label.color = color;
            group.alpha = 1f;

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(FadeAfterDelay());
        }

        IEnumerator FadeAfterDelay()
        {
            yield return new WaitForSecondsRealtime(2f);

            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(1f - t / 0.4f);
                yield return null;
            }
            group.alpha = 0f;
        }
    }
}
