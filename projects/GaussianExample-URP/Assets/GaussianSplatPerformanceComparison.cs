using System.Collections;
using UnityEngine;

/// <summary>
/// Measures original 3DGS and sort-free GS in the same scene. Only one root is
/// enabled at a time because their composite operations share GaussianSplatRT.
/// </summary>
public sealed class GaussianSplatPerformanceComparison : MonoBehaviour
{
    [Header("One original-3DGS root and one sort-free-GS root")]
    [SerializeField] GameObject m_Original3DGS;
    [SerializeField] GameObject m_SortFreeGS;

    [Header("Frame pacing")]
    [Tooltip("Use -1 for no application cap. VSync remains disabled while enabled.")]
    [SerializeField] int m_TargetFrameRate = -1;

    [Header("Benchmark")]
    [SerializeField, Min(0f)] float m_WarmupSeconds = 2f;
    [SerializeField, Min(0.25f)] float m_SampleSeconds = 5f;
    [SerializeField] bool m_RunOnStart = true;

    string m_OriginalResult = "not measured";
    string m_SortFreeResult = "not measured";
    int m_PreviousVSync;
    int m_PreviousTargetFrameRate;
    Coroutine m_Benchmark;

    void OnEnable()
    {
        m_PreviousVSync = QualitySettings.vSyncCount;
        m_PreviousTargetFrameRate = Application.targetFrameRate;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = m_TargetFrameRate;
    }

    void Start()
    {
        SelectOriginal();
        if (m_RunOnStart) RunBenchmark();
    }

    void OnDisable()
    {
        if (m_Benchmark != null) StopCoroutine(m_Benchmark);
        QualitySettings.vSyncCount = m_PreviousVSync;
        Application.targetFrameRate = m_PreviousTargetFrameRate;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectOriginal();
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSortFree();
        if (Input.GetKeyDown(KeyCode.B)) RunBenchmark();
    }

    public void SelectOriginal() => SetActiveRenderer(m_Original3DGS, m_SortFreeGS);
    public void SelectSortFree() => SetActiveRenderer(m_SortFreeGS, m_Original3DGS);

    public void RunBenchmark()
    {
        if (m_Benchmark != null) StopCoroutine(m_Benchmark);
        m_Benchmark = StartCoroutine(Benchmark());
    }

    IEnumerator Benchmark()
    {
        yield return Measure(m_Original3DGS, m_SortFreeGS, value => m_OriginalResult = value);
        yield return Measure(m_SortFreeGS, m_Original3DGS, value => m_SortFreeResult = value);
        m_Benchmark = null;
    }

    IEnumerator Measure(GameObject enabledRenderer, GameObject disabledRenderer, System.Action<string> setResult)
    {
        SetActiveRenderer(enabledRenderer, disabledRenderer);
        yield return new WaitForSecondsRealtime(m_WarmupSeconds);

        float elapsed = 0f;
        int frames = 0;
        while (elapsed < m_SampleSeconds)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            ++frames;
        }

        setResult($"{frames / elapsed:F1} FPS  ({elapsed * 1000f / frames:F2} ms/frame)");
    }

    static void SetActiveRenderer(GameObject enabledRenderer, GameObject disabledRenderer)
    {
        if (disabledRenderer != null) disabledRenderer.SetActive(false);
        if (enabledRenderer != null) enabledRenderer.SetActive(true);
    }

    void OnGUI() => GUI.Label(new Rect(16, 16, 480, 110),
        $"3D Gaussian Splatting: {m_OriginalResult}\n" +
        $"Sort-free Gaussian Splatting: {m_SortFreeResult}\n" +
        "1: original   2: sort-free   B: benchmark again\n" +
        $"VSync: off, target: {(m_TargetFrameRate < 0 ? "unlimited" : m_TargetFrameRate + " FPS")}");
}
