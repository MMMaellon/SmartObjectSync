using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.Udon;

[DefaultExecutionOrder(31000)]
public class PostLateUpdater : MonoBehaviour
{
    public UdonManager udonManager;

    private void LateUpdate()
    {
        udonManager.PostLateUpdate();
    }
}