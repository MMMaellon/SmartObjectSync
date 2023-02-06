// GlobalProfileHandler.cs
#define AVERAGE_OUTPUT

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[DefaultExecutionOrder(1000000000)]
public class GlobalProfileHandler : UdonSharpBehaviour
{
    public TMPro.TextMeshProUGUI _timeText;
    private GlobalProfileKickoff _kickoff;

    private void Start()
    {
        _kickoff = GetComponent<GlobalProfileKickoff>();
    }

    private int _currentFrame = -1;
    private float _elapsedTime = 0f;
#if AVERAGE_OUTPUT
    private float _measuredFrametimeTotal = 0f;
    private float _measuredTimeTotal = 0f;
    private int _measuredTimeFrameCount = 0;
    private const int MEASURE_FRAME_AMOUNT = 45;
#endif

    private void FixedUpdate()
    {
        if (_currentFrame != Time.frameCount)
        {
            _elapsedTime = 0f;
            _currentFrame = Time.frameCount;
        }

        if (_kickoff)
            _elapsedTime += (float)_kickoff.stopwatch.Elapsed.TotalSeconds * 1000f;
    }

    private void Update()
    {
        if (_currentFrame != Time.frameCount) // FixedUpdate didn't run this frame, so reset the time
            _elapsedTime = 0f;

        _elapsedTime += (float)_kickoff.stopwatch.Elapsed.TotalSeconds * 1000f;
    }

    private void LateUpdate()
    {
        _elapsedTime += (float)_kickoff.stopwatch.Elapsed.TotalSeconds * 1000f;
    }

    float lastFrame;

    public override void PostLateUpdate()
    {
        _elapsedTime += (float)_kickoff.stopwatch.Elapsed.TotalSeconds * 1000f;
        float now = Time.timeSinceLevelLoad;
        _measuredFrametimeTotal += (now - lastFrame) * 1000f;
        lastFrame = now;

#if AVERAGE_OUTPUT
        if (_measuredTimeFrameCount >= MEASURE_FRAME_AMOUNT)
        {
            _timeText.text = $"Udon: {(_measuredTimeTotal / _measuredTimeFrameCount):F4}ms\nFrametime: {(_measuredFrametimeTotal / _measuredTimeFrameCount):F4}ms";
            _measuredTimeTotal = 0f;
            _measuredFrametimeTotal = 0f;
            _measuredTimeFrameCount = 0;
        }
        _measuredTimeTotal += _elapsedTime;
        _measuredTimeFrameCount += 1;
#else
        _timeText.text = $"Update time:\n{_elapsedTime:F4}ms";
#endif
    }
}