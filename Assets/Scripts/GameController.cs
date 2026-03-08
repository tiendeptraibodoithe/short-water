using System;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Bottles")]
    public BottleController[] allBottles;
    [HideInInspector] public int numberOfColors;
    public static event Action OnWin;
    public static event Action OnLose;

    private BottleController FirstBottle;
    private BottleController SecondBottle;
    private bool gameOver = false;
    private int _activeTransfers = 0;  // Số animation đang chạy — chỉ check lose khi về 0
    private int _bottlesToComplete;

    // Lịch sử trạng thái để phát hiện cycle (đổ qua đổi lại)
    private System.Collections.Generic.HashSet<string> visitedStates = new System.Collections.Generic.HashSet<string>();

    

    void Start()
    {
        if (allBottles == null || allBottles.Length == 0)
        {
            allBottles = FindObjectsOfType<BottleController>();
            Debug.Log($"[GameController] Auto-found {allBottles.Length} bottles.");
        }

        // Đếm số chai có màu (không rỗng) → đây là số chai cần hoàn thành để thắng
        _bottlesToComplete = 0;
        foreach (var b in allBottles)
            if (b.numberOfColorsInBottle > 0) _bottlesToComplete++;
        Debug.Log($"[GameController] Bottles to complete: {_bottlesToComplete}");

        visitedStates.Add(GetGameStateHash());
    }

    void Update()
    {
        if (gameOver) return;
        // Không block input — vẫn cho click trong lúc animation

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null)
            {
                BottleController clickedBottle = hit.collider.GetComponent<BottleController>();
                if (clickedBottle == null) return;
                // Không cho chọn bottle đã hoàn thành
                if (clickedBottle.isComplete) return;

                if (FirstBottle == null)
                {
                    // Bottle rỗng không thể là nguồn đổ nước
                    if (clickedBottle.numberOfColorsInBottle == 0) return;

                    // Chưa có bottle nào được chọn → chọn bottle này
                    FirstBottle = clickedBottle;
                    FirstBottle.Select();
                }
                else if (clickedBottle == FirstBottle)
                {
                    // Click lại chính bottle đang chọn → bỏ chọn
                    FirstBottle.Deselect();
                    FirstBottle = null;
                }
                else
                {
                    // Click sang bottle khác
                    SecondBottle = clickedBottle;
                    FirstBottle.bottleControllerRef = SecondBottle;

                    FirstBottle.UpdateTopColorValues();
                    SecondBottle.UpdateTopColorValues();

                    if (SecondBottle.FillBottleCheck(FirstBottle.topColor))
                    {
                        // Có thể đổ → tăng counter, bất đầu đổ
                        _activeTransfers++;
                        FirstBottle.Deselect();
                        FirstBottle.StartColorTransfer(OnTransferComplete);
                        FirstBottle = null;
                        SecondBottle = null;
                    }
                    else
                    {
                        // Không thể đổ → bỏ chọn bottle cũ, chọn bottle mới
                        FirstBottle.Deselect();
                        FirstBottle = SecondBottle;
                        SecondBottle = null;
                        FirstBottle.Select();
                    }
                }
            }
            else
            {
                // Click ra ngoài → bỏ chọn
                if (FirstBottle != null)
                {
                    FirstBottle.Deselect();
                    FirstBottle = null;
                }
            }
        }
    }

    // ── Callback sau khi 1 animation đổ xong ─────────────────────
    void OnTransferComplete()
    {
        if (gameOver) return;
        _activeTransfers--;

        // CheckWin ngay — isComplete được set chính xác sau mỗi lần đổ
        if (CheckWin())
        {
            gameOver = true;
            OnWin?.Invoke();
            return;
        }

        // Chỉ check lose/visited khi tất cả animation đã xong
        if (_activeTransfers > 0) return;

        // Phát hiện cycle: trạng thái này đã từng xuất hiện?
        string state = GetGameStateHash();
        if (visitedStates.Contains(state))
        {
            gameOver = true;
            OnLose?.Invoke();
            return;
        }
        visitedStates.Add(state);

        if (CheckLose())
        {
            gameOver = true;
            OnLose?.Invoke();
        }
    }

    // Hash trạng thái game: màu sắc từng lọ theo thứ tự từ dưới lên
    string GetGameStateHash()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var b in allBottles)
        {
            sb.Append('|');
            for (int i = 0; i < b.numberOfColorsInBottle; i++)
                sb.Append(ColorUtility.ToHtmlStringRGBA(b.bottleColors[i]));
        }
        return sb.ToString();
    }

    // Thắng: tất cả chai có màu ban đầu đều đã isComplete
    bool CheckWin()
    {
        int completed = 0;
        foreach (var b in allBottles)
            if (b.isComplete) completed++;
        return completed >= _bottlesToComplete;
    }

    // Thua: không còn bottle nào có thể đổ sang bottle khác
    bool CheckLose()
    {
        foreach (var src in allBottles)
        {
            if (src.isComplete) continue;
            if (src.numberOfColorsInBottle == 0) continue;

            src.UpdateTopColorValues();

            foreach (var dst in allBottles)
            {
                if (dst == src) continue;
                if (dst.isComplete) continue;
                if (dst.FillBottleCheck(src.topColor))
                    return false;   // còn ít nhất 1 nước có thể đổ → chưa thua
            }
        }
        return true;    // không còn nước nào đổ được → thua
    }

}
