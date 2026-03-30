using UnityEngine;

/// <summary>
/// このスクリプトは「ドラッグ入力」を受け取る担当。
///
/// 役割は大きく3つ。
/// 1. DragArea の上でマウス / タッチが始まったか調べる
/// 2. ドラッグ開始位置と終了位置を記録する
/// 3. その結果を GameManager に渡す
///
/// ポイント:
/// - DragArea は「入力を受けるための場所」
/// - Board は「実際に墓を飛ばす場所」
/// - この2つは別の位置にあるので、
///   DragArea上の位置を Board上の位置に変換している
/// </summary>
public class DragAreaController : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;   // 最終的に結果を渡す相手
    public Camera mainCamera;         // 画面のクリック位置から Ray を飛ばすためのカメラ
    public Transform boardTransform;  // 実際の盤面の Transform

    [Header("Area Sizes")]
    // DragArea の「実際の横幅・縦幅」
    // ここは見た目の scale と対応させる
    public float dragAreaWidth = 13f;
    public float dragAreaHeight = 5f;

    // Board の「実際の横幅・縦幅」
    public float boardWidth = 13f;
    public float boardHeight = 13f;

    // Board の表面の Y 座標
    // 盤面が y=5 にあるなら 5
    public float boardSurfaceY = 5f;

    [Header("State")]
    private bool dragging = false;    // 今ドラッグ中かどうか
    private bool draggable = true;    // 今このターンでドラッグ可能かどうか

    // ドラッグ開始した時間
    // 速さ(speed)を出すときに使う
    private float dragStartTime;

    // 「実際に墓を飛ばす開始地点」
    // DragArea上の座標を Board上の座標に変換したもの
    private Vector3 launchWorldPos;

    // 1ターンで1回だけドラッグを許可するためのフラグ
    private bool hasDragged = false;

    /// <summary>
    /// GameManager から呼ばれる。
    /// ターン開始時にドラッグ可能にしたり、
    /// ターン終了時にドラッグ不能にしたりする。
    /// </summary>
    public void SetDraggable(bool value)
    {
        draggable = value;

        // 新しいターンで操作可能になったら、
        // 「もうドラッグしたか」の記録をリセットする
        if (value)
        {
            hasDragged = false;
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        // エディタ上ではマウス操作を使う
        HandleMouse();
#else
        // 実機ではタッチ操作を使う
        HandleTouch();
#endif
    }

    /// <summary>
    /// マウス入力版
    /// </summary>
    void HandleMouse()
    {
        if (!draggable) return;

        // マウスを押した瞬間
        if (Input.GetMouseButtonDown(0))
        {
            // 押した場所が DragArea 上ならドラッグ開始
            if (RaycastDragArea(Input.mousePosition))
            {
                StartDrag(Input.mousePosition);
            }
        }
        // マウスを離した瞬間
        else if (Input.GetMouseButtonUp(0) && dragging)
        {
            EndDrag(Input.mousePosition);
        }
    }

    /// <summary>
    /// タッチ入力版
    /// </summary>
    void HandleTouch()
    {
        if (!draggable || Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            if (RaycastDragArea(touch.position))
            {
                StartDrag(touch.position);
            }
        }
        else if (touch.phase == TouchPhase.Ended && dragging)
        {
            EndDrag(touch.position);
        }
    }

    /// <summary>
    /// ドラッグ開始時の処理
    ///
    /// ここでやっていること:
    /// 1. ドラッグ開始を記録
    /// 2. クリックした DragArea 上の位置を取る
    /// 3. その位置を Board 上の位置に変換する
    /// 4. それを「発射位置」として保存する
    /// </summary>
    void StartDrag(Vector2 screenPos)
    {
        // 1ターンで2回目以降は受け付けない
        if (hasDragged) return;

        dragging = true;
        dragStartTime = Time.time;

        // まずは DragArea 上のワールド座標を取る
        if (TryGetDragAreaPoint(screenPos, out Vector3 dragAreaPoint))
        {
            // DragArea 上の座標を Board 上の座標に変換
            launchWorldPos = ConvertDragAreaPointToBoardPoint(dragAreaPoint);

            Debug.Log($"▶ Drag Start / {launchWorldPos}");
        }
        else
        {
            // もし取得できなかったらドラッグ開始失敗
            dragging = false;
        }
    }

    /// <summary>
    /// ドラッグ終了時の処理
    ///
    /// ここでやっていること:
    /// 1. 終了位置を DragArea 上で取る
    /// 2. Board 上の位置に変換する
    /// 3. 開始位置との差(delta)を出す
    /// 4. そこから「向き」「距離」「速さ」を作る
    /// 5. GameManager に渡す
    /// </summary>
    void EndDrag(Vector2 screenPos)
    {
        dragging = false;

        // 終了位置を DragArea 上で取得
        if (!TryGetDragAreaPoint(screenPos, out Vector3 dragAreaEndPoint))
        {
            Debug.Log("▶ DragArea取得失敗 → キャンセル");
            return;
        }

        // 終了位置も Board 上に変換
        Vector3 endWorldPos = ConvertDragAreaPointToBoardPoint(dragAreaEndPoint);

        // 開始位置 → 終了位置 の差を出す
        Vector3 worldDelta = endWorldPos - launchWorldPos;
        worldDelta.y = 0f; // 上下方向は無視して平面だけ見る

        float worldDistance = worldDelta.magnitude;
        float dragTime = Time.time - dragStartTime;

        // あまりに短いドラッグは無効
        if (worldDistance < 0.05f)
        {
            Debug.Log("▶ ドラッグ短すぎ → キャンセル");
            return;
        }

        hasDragged = true;

        // x,z を 2D の向きとして使う
        Vector2 dir2D = new Vector2(worldDelta.x, worldDelta.z).normalized;

        // ドラッグの長さ = 飛距離の強さ
        float distance = worldDistance;

        // 一応 speed も出している
        float speed = distance / Mathf.Max(dragTime, 0.01f);

        Debug.Log($"▶ Drag End / start={launchWorldPos}, end={endWorldPos}, delta={worldDelta}");

        // ここで GameManager に「発射していいよ」と伝える
        gameManager.OnShakeRelease(
            launchWorldPos, // 発射位置
            dir2D,          // 振る方向
            distance,       // ドラッグの長さ
            speed           // ドラッグの速さ
        );
    }

    /// <summary>
    /// 画面上の位置(screenPos)から、
    /// DragArea 上のワールド座標を取得する
    ///
    /// やっていること:
    /// - 画面のクリック位置から Ray を飛ばす
    /// - その Ray が DragArea に当たったら hit.point を返す
    /// </summary>
    bool TryGetDragAreaPoint(Vector2 screenPos, out Vector3 dragAreaPoint)
    {
        dragAreaPoint = Vector3.zero;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider.gameObject == gameObject)
            {
                dragAreaPoint = hit.point;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// DragArea 上の座標を、
    /// Board 上の対応する座標に変換する
    ///
    /// イメージ:
    /// DragArea の左端を触ったら Board の左端
    /// DragArea の右端を触ったら Board の右端
    ///
    /// つまり「割合」で変換している
    /// </summary>
    Vector3 ConvertDragAreaPointToBoardPoint(Vector3 dragPoint)
    {
        // DragArea の中心から見たローカル座標を出す
        float localX = dragPoint.x - transform.position.x;
        float localZ = dragPoint.z - transform.position.z;

        // DragArea 内での位置を 0～1 の割合に変換
        float normalizedX = (localX + dragAreaWidth * 0.5f) / dragAreaWidth;
        float normalizedZ = (localZ + dragAreaHeight * 0.5f) / dragAreaHeight;

        // 万が一はみ出しても 0～1 に収める
        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedZ = Mathf.Clamp01(normalizedZ);

        // その割合を Board の大きさに合わせて変換
        float boardLocalX = (normalizedX - 0.5f) * boardWidth;
        float boardLocalZ = (normalizedZ - 0.5f) * boardHeight;

        // 最終的な Board 上のワールド座標を返す
        return new Vector3(
            boardTransform.position.x + boardLocalX,
            boardSurfaceY,
            boardTransform.position.z + boardLocalZ
        );
    }

    /// <summary>
    /// 「今クリックしている場所が本当に DragArea 上か？」を調べる
    /// </summary>
    bool RaycastDragArea(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        return Physics.Raycast(ray, out RaycastHit hit, 100f)
            && hit.collider.gameObject == gameObject;
    }
}