using System;
using UnityEngine;

public class SwipeDetector : MonoBehaviour, IInputDetector
{

    private SwipeState swipeState = SwipeState.SwipeNotStarted;
    private Vector2 startPoint;
    private DateTime timeSwipeStarted;
    private TimeSpan maxSwipeDuration = TimeSpan.FromSeconds(1);
    private TimeSpan minSwipeDuration = TimeSpan.FromMilliseconds(100);
    private const float MinSwipeDistance = 30f;

    public SwipeDirection? DetectInputDirection()
    {
        // 手机上优先读取触摸事件，避免同一次触摸再被模拟的鼠标事件处理一次。
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                BeginSwipe(touch.position);
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                return EndSwipe(touch.position);
            }
            else if (touch.phase == TouchPhase.Canceled)
            {
                swipeState = SwipeState.SwipeNotStarted;
            }

            return null;
        }

        if (Input.GetMouseButtonDown(0))
            BeginSwipe(Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
            return EndSwipe(Input.mousePosition);

        return null;
    }

    private void BeginSwipe(Vector2 position)
    {
        timeSwipeStarted = DateTime.Now;
        swipeState = SwipeState.SwipeStarted;
        startPoint = position;
    }

    private SwipeDirection? EndSwipe(Vector2 position)
    {
        if (swipeState != SwipeState.SwipeStarted) return null;

        swipeState = SwipeState.SwipeNotStarted;
        TimeSpan timeDifference = DateTime.Now - timeSwipeStarted;
        if (timeDifference < minSwipeDuration || timeDifference > maxSwipeDuration)
            return null;

        Vector2 differenceVector = position - startPoint;
        if (differenceVector.sqrMagnitude < MinSwipeDistance * MinSwipeDistance)
            return null;

        if (differenceVector.x * differenceVector.x >= differenceVector.y * differenceVector.y)
            return differenceVector.x < 0 ? SwipeDirection.Left : SwipeDirection.Right;

        return differenceVector.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
    }
}
