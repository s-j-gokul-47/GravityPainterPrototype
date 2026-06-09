using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputManager : MonoBehaviour
{
    private const float RaycastDistance = 500f;
    private const float DoubleTapMaxDelay = 0.4f;

    private Camera mainCamera;
    private BallController _ball;
    private int _lastSideTapTileId = -1;
    private float _lastSideTapUnscaledTime = -10f;
    private Coroutine _pendingSidePaintRoutine;
    private TileZone _pendingPaintTile;
    private Vector3 _pendingPaintPoint;

    private void Start()
    {
        mainCamera = Camera.main;
        _ball = FindFirstObjectByType<BallController>();
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            return;
        }

        if (!TryGetPrimaryPointerDown(out Vector2 screenPosition))
        {
            return;
        }

        HandleTap(screenPosition);
    }

    private void HandleTap(Vector2 screenPosition)
    {
        if (!TryRaycastTile(screenPosition, out TileZone tile, out Vector3 hitPoint))
        {
            CancelPendingSidePaint();
            ResetSideDoubleTap();
            return;
        }

        if (tile.IsLeftSideTap(hitPoint))
        {
            HandleSideTap(tile, hitPoint, toLeftEdge: true);
            return;
        }

        if (tile.IsRightSideTap(hitPoint))
        {
            HandleSideTap(tile, hitPoint, toLeftEdge: false);
            return;
        }

        CancelPendingSidePaint();
        ResetSideDoubleTap();
        tile.SetZoneFromWorldPoint(hitPoint);
    }

    private void HandleSideTap(TileZone tile, Vector3 hitPoint, bool toLeftEdge)
    {
        int tileId = tile.gameObject.GetInstanceID();
        float now = Time.unscaledTime;
        bool isDoubleTap = tileId == _lastSideTapTileId
            && now - _lastSideTapUnscaledTime <= DoubleTapMaxDelay;

        if (isDoubleTap)
        {
            CancelPendingSidePaint();
            TryStartCrossSlide(tile, toLeftEdge);
            ResetSideDoubleTap();
            return;
        }

        _lastSideTapTileId = tileId;
        _lastSideTapUnscaledTime = now;
        ScheduleSidePaint(tile, hitPoint);
    }

    private void ScheduleSidePaint(TileZone tile, Vector3 hitPoint)
    {
        CancelPendingSidePaint();
        _pendingPaintTile = tile;
        _pendingPaintPoint = hitPoint;
        _pendingSidePaintRoutine = StartCoroutine(PaintSideAfterDelay());
    }

    private IEnumerator PaintSideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(DoubleTapMaxDelay);
        if (_pendingPaintTile != null)
        {
            _pendingPaintTile.SetZoneFromWorldPoint(_pendingPaintPoint);
        }

        _pendingSidePaintRoutine = null;
        _pendingPaintTile = null;
    }

    private void CancelPendingSidePaint()
    {
        if (_pendingSidePaintRoutine != null)
        {
            StopCoroutine(_pendingSidePaintRoutine);
            _pendingSidePaintRoutine = null;
        }

        _pendingPaintTile = null;
    }

    private bool TryStartCrossSlide(TileZone tile, bool toLeftEdge)
    {
        if (_ball == null)
        {
            _ball = FindFirstObjectByType<BallController>();
        }

        if (_ball == null || !_ball.IsOnTileForCross(tile))
        {
            return false;
        }

        _ball.StartCrossSlideToEdge(tile, toLeftEdge);
        return true;
    }

    private void ResetSideDoubleTap()
    {
        _lastSideTapTileId = -1;
        _lastSideTapUnscaledTime = -10f;
    }

    private bool TryRaycastTile(Vector2 screenPosition, out TileZone tile, out Vector3 hitPoint)
    {
        tile = null;
        hitPoint = default;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            RaycastDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);

        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null)
            {
                continue;
            }

            if (col.GetComponentInParent<BallController>() != null)
            {
                continue;
            }

            TileZone hitZone = col.GetComponent<TileZone>() ?? col.GetComponentInParent<TileZone>();
            tile = TileZone.GetPrimaryZone(hitZone != null ? hitZone.gameObject : null);
            if (tile != null)
            {
                hitPoint = hits[i].point;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetPrimaryPointerDown(out Vector2 screenPosition)
    {
        screenPosition = default;

        Touchscreen touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = touch.primaryTouch.position.ReadValue();
            return true;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPosition = mouse.position.ReadValue();
            return true;
        }

        return false;
    }
}
