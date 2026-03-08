using UnityEngine;
using DG.Tweening;

public static class PanelAnimator
{
    public static void Show(GameObject panel, float duration = 0.3f)
    {
        if (panel == null) return;
        panel.transform.localScale = Vector3.zero;
        panel.SetActive(true);
        panel.transform
            .DOScale(Vector3.one, duration)
            .SetEase(Ease.OutBack);
    }

    public static void Hide(GameObject panel, float duration = 0.25f)
    {
        if (panel == null) return;
        panel.transform
            .DOScale(Vector3.zero, duration)
            .SetEase(Ease.InBack)
            .OnComplete(() => panel.SetActive(false));
    }
}
