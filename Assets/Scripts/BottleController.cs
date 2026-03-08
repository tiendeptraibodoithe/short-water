using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BottleController : MonoBehaviour
{
    public Color[] bottleColors;
    public SpriteRenderer bottleMask;
    public AnimationCurve ScaleAndRotationCurve;
    public AnimationCurve FillAmountCurve;
    public AnimationCurve RotationSpeedMultiplier;

    public BottleController bottleControllerRef;

    public bool justThisBottle = false;

    private int numberOfColorsToTransfer = 0;

    public float[] fillAmounts;
    public float[] rotationValues;
    private int rotationIndex = 0;
    [Range(0,4)] 
    public int numberOfColorsInBottle = 4;
    public Color topColor;
    public int numberOfTopColorLayers = 1;
    public float timeToRotate = 1f;
    bool isRotating = false;
    public Transform leftRotationPoint;
    public Transform rightRotationPoint;
    private Transform chosenRotationPoint;
    Vector3 originalPosition;
    Vector3 startPosition;
    Vector3 endPosition;
    public LineRenderer lineRenderer;

    // ── Selection ──────────────────────────────────────────
    private static BottleController selectedBottle = null;
    private bool isSelected = false;
    private Coroutine selectMoveCoroutine;
    public float selectOffsetY = 0.5f;
    public float selectMoveDuration = 0.15f;
    // ───────────────────────────────────────────────────────

    // ── Completion ─────────────────────────────────────────
    [HideInInspector] public bool isComplete = false;
    public GameObject completionIndicator;
    // ───────────────────────────────────────────────────────
    private Action onTransferComplete;

    private float directionMultiplier = 1f;

    private const int sortingOrderBoost = 10;

    void Start()
    {
        originalPosition = transform.position;
        bottleMask.material.SetFloat("_FillAmount", fillAmounts[numberOfColorsInBottle]);
        UpdateColorsOnShader();
        UpdateTopColorValues();
        if (completionIndicator != null)
            completionIndicator.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && justThisBottle)
        {
            UpdateTopColorValues();
            if (bottleControllerRef.FillBottleCheck(topColor))
            {
                ChoseRotationPointAndDirection();
                numberOfColorsToTransfer = Mathf.Min(numberOfTopColorLayers, 4 - bottleControllerRef.numberOfColorsInBottle);
                for(int i = 0; i < numberOfColorsToTransfer; i++)
                {
                    bottleControllerRef.bottleColors[bottleControllerRef.numberOfColorsInBottle + i] = topColor;
                }
                bottleControllerRef.UpdateColorsOnShader();
                CalculateRotationIndex(4 - bottleControllerRef.numberOfColorsInBottle);
                StartCoroutine(MoveBottle());
            }
        }
    }

    public void Select()
    {
        if (isSelected) return;
        isSelected = true;
        if (selectMoveCoroutine != null) StopCoroutine(selectMoveCoroutine);
        selectMoveCoroutine = StartCoroutine(SmoothMove(
            transform.position,
            originalPosition + Vector3.up * selectOffsetY
        ));
    }

    public void Deselect()
    {
        if (!isSelected) return;
        isSelected = false;
        if (selectedBottle == this) selectedBottle = null;
        if (selectMoveCoroutine != null) StopCoroutine(selectMoveCoroutine);
        selectMoveCoroutine = StartCoroutine(SmoothMove(
            transform.position,
            originalPosition
        ));
    }

    IEnumerator SmoothMove(Vector3 from, Vector3 to)
    {
        float t = 0f;
        while (t < selectMoveDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / selectMoveDuration);
            float eased = 1f - (1f - normalized) * (1f - normalized);
            transform.position = Vector3.Lerp(from, to, eased);
            yield return null;
        }
        transform.position = to;
        if (!isSelected)
            originalPosition = to;
    }

    

    public void StartColorTransfer(Action onComplete = null)
    {
        onTransferComplete = onComplete;
        ChoseRotationPointAndDirection();
        numberOfColorsToTransfer = Mathf.Min(numberOfTopColorLayers, 4 - bottleControllerRef.numberOfColorsInBottle);
        for(int i = 0; i < numberOfColorsToTransfer; i++)
        {
            bottleControllerRef.bottleColors[bottleControllerRef.numberOfColorsInBottle + i] = topColor;
        }
        bottleControllerRef.UpdateColorsOnShader();
        CalculateRotationIndex(4 - bottleControllerRef.numberOfColorsInBottle);
        SetSortingOrderBoost(sortingOrderBoost);
        StartCoroutine(MoveBottle());
    }

    void SetSortingOrderBoost(int boost)
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.sortingOrder += boost;
    }

    void UpdateColorsOnShader()
    {
        bottleMask.material.SetColor("_Color1", bottleColors[0]);
        bottleMask.material.SetColor("_Color2", bottleColors[1]);
        bottleMask.material.SetColor("_Color3", bottleColors[2]);
        bottleMask.material.SetColor("_Color4", bottleColors[3]);
    }

    IEnumerator RotateBottle()
    {
        float t = 0f;
        float lerpValue;
        float angleValue;
        float lastAngleValue = 0f;
        while (t < timeToRotate)
        {
            lerpValue = t/timeToRotate;
            angleValue = Mathf.Lerp(0f, directionMultiplier * rotationValues[rotationIndex], lerpValue);
            transform.RotateAround(chosenRotationPoint.position, Vector3.forward, lastAngleValue - angleValue);
            bottleMask.material.SetFloat("_ScaleAndRotation", ScaleAndRotationCurve.Evaluate(angleValue));
            if(fillAmounts[numberOfColorsInBottle] > FillAmountCurve.Evaluate(angleValue) + 0.005f)
            // bottleMask.material.SetFloat("_ScaleAndRotation", ScaleAndRotationCurve.Evaluate(Mathf.Abs(angleValue)));
            // if(fillAmounts[numberOfColorsInBottle] > FillAmountCurve.Evaluate(Mathf.Abs(angleValue)))
            {
                if(lineRenderer.enabled == false)
                {
                    Color lineColor = topColor;
                    lineColor.a = 1f;
                    lineRenderer.startColor = lineColor;
                    lineRenderer.endColor = lineColor;

                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition(0, chosenRotationPoint.position);
                    lineRenderer.SetPosition(1, chosenRotationPoint.position - Vector3.up * 2.5f);

                    lineRenderer.enabled = true;

                    // Nước bắt đầu chảy → bật âm thanh loop
                    SoundManager.Instance?.PlaySFXLoop(SoundType.WaterPour);
                }
                bottleMask.material.SetFloat("_FillAmount", FillAmountCurve.Evaluate(angleValue));
                
                bottleControllerRef.FillUp(FillAmountCurve.Evaluate(lastAngleValue)-FillAmountCurve.Evaluate(angleValue));
            }
            t += Time.deltaTime * RotationSpeedMultiplier.Evaluate(angleValue);
            lastAngleValue = angleValue;
            yield return new WaitForEndOfFrame();
        }
        angleValue = directionMultiplier * rotationValues[rotationIndex];
        bottleMask.material.SetFloat("_ScaleAndRotation", ScaleAndRotationCurve.Evaluate(angleValue));
        bottleMask.material.SetFloat("_FillAmount", FillAmountCurve.Evaluate(angleValue));
        numberOfColorsInBottle -= numberOfColorsToTransfer;

        // Nước ngừng chảy → dừng âm thanh loop ngay lập tức
        SoundManager.Instance?.StopSFXLoop();

        bottleControllerRef.numberOfColorsInBottle += numberOfColorsToTransfer;
        lineRenderer.enabled = false;

        // Cập nhật topColor/numberOfTopColorLayers cho bottle nhận trước khi kiểm tra hoàn thành
        bottleControllerRef.UpdateTopColorValues();
        bottleControllerRef.CheckCompletion();

        // Nếu đây là lớp màu cuối cùng, ẩn bottleMask để tránh hiện lại màu khi xoay về
        bool isLastColor = numberOfColorsInBottle <= 0;
        StartCoroutine(RotateBottleBack(isLastColor));
    }

    IEnumerator RotateBottleBack(bool hideBottleMask = false)
    {
        // Ẩn bottleMask nếu đây là lớp màu cuối cùng
        if (hideBottleMask)
            bottleMask.enabled = false;

        float t = 0f;
        float lerpValue;
        float angleValue;
        float lastAngleValue = directionMultiplier * rotationValues[rotationIndex];
        while (t < timeToRotate)
        {
            lerpValue = t/timeToRotate;
            angleValue = Mathf.Lerp(directionMultiplier * rotationValues[rotationIndex], 0f, lerpValue);
            transform.RotateAround(chosenRotationPoint.position, Vector3.forward, lastAngleValue - angleValue);
            // bottleMask.material.SetFloat("_ScaleAndRotation", ScaleAndRotationCurve.Evaluate(Mathf.Abs(angleValue)));
            bottleMask.material.SetFloat("_ScaleAndRotation", ScaleAndRotationCurve.Evaluate(angleValue));
            lastAngleValue = angleValue;
            t += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        UpdateTopColorValues();
        angleValue = 0f;
        transform.eulerAngles = new Vector3(0,0,angleValue);
        bottleMask.material.SetFloat("_ScaleAndRotation", ScaleAndRotationCurve.Evaluate(angleValue));

        // Bật lại bottleMask sau khi xoay về xong
        if (hideBottleMask)
            bottleMask.enabled = true;

        StartCoroutine(MoveBottleBack());
    }

    IEnumerator MoveBottle()
    {
        startPosition = transform.position;
        if(chosenRotationPoint == leftRotationPoint)
        {
            endPosition = bottleControllerRef.rightRotationPoint.position;
        }
        else
        {
            endPosition = bottleControllerRef.leftRotationPoint.position;
        }

        float t = 0;

        while(t < 1)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            t += Time.deltaTime * 2;
            yield return new WaitForEndOfFrame();
        }

        transform.position = endPosition;
        StartCoroutine(RotateBottle());
    }

    IEnumerator MoveBottleBack()
    {
        startPosition = transform.position;
        endPosition = originalPosition;

        float t = 0;

        while(t < 1)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            t += Time.deltaTime * 2;
            yield return new WaitForEndOfFrame();
        }

        transform.position = endPosition;
        SetSortingOrderBoost(-sortingOrderBoost);

        // Gọi callback sau khi hoàn tất toàn bộ animation
        onTransferComplete?.Invoke();
        onTransferComplete = null;
    }

    public void UpdateTopColorValues()
    {
        if (numberOfColorsInBottle <= 0)
        {
            numberOfTopColorLayers = 0;
            return;
        }

        topColor = bottleColors[numberOfColorsInBottle - 1];
        numberOfTopColorLayers = 1;

        for (int i = numberOfColorsInBottle - 2; i >= 0; i--)
        {
            if (ColorsMatch(bottleColors[i], topColor))
                numberOfTopColorLayers++;
            else
                break;
        }

        rotationIndex = 3 - (numberOfColorsInBottle - numberOfTopColorLayers);
    }

    public void CheckCompletion()
    {
        if (isComplete) return;

        // Hoàn thành khi đủ 4 lớp và tất cả cùng màu
        if (numberOfColorsInBottle == 4 && numberOfTopColorLayers == 4)
        {
            isComplete = true;
            if (completionIndicator != null)
            {
                completionIndicator.transform.localPosition = new Vector3(0f, 4f, 0f);
                completionIndicator.SetActive(true);
                completionIndicator.transform.DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.OutBack);
            }

            // Phát BottleClose trước, chờ hết clip rồi mới phát BottleFull
            SoundManager.Instance?.PlaySFX(SoundType.BottleClose);
            float delay = SoundManager.Instance?.GetClipLength(SoundType.BottleClose) ?? 0f;
            DOVirtual.DelayedCall(delay, () =>
            {
                SoundManager.Instance?.PlaySFX(SoundType.BottleFull);
            });
        }
    }

    bool ColorsMatch(Color a, Color b)
    {
        float tolerance = 0.01f;
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance &&
               Mathf.Abs(a.a - b.a) < tolerance;
    }

    public bool FillBottleCheck(Color colorToCheck)
    {
        if(numberOfColorsInBottle == 0)
        {
            return true;
        }
        else
        {
            if(numberOfColorsInBottle == 4)
            {
                return false;
            }
            else
            {
                if(ColorsMatch(topColor, colorToCheck))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }

    private void CalculateRotationIndex(int numberOfEmptySpacesInSecondBottle)
    {
        rotationIndex = 3 - (numberOfColorsInBottle - Mathf.Min(numberOfEmptySpacesInSecondBottle, numberOfTopColorLayers));
    }
    private void FillUp(float fillAmountToAdd)
    {
        bottleMask.material.SetFloat("_FillAmount", bottleMask.material.GetFloat("_FillAmount") + fillAmountToAdd);
    }
    private void ChoseRotationPointAndDirection()
    {
        if(transform.position.x > bottleControllerRef.transform.position.x)
        {
            chosenRotationPoint = leftRotationPoint;
            directionMultiplier = -1f;
        }
        else
        {
            chosenRotationPoint = rightRotationPoint;
            directionMultiplier = 1f;
        }
        
    }
}
