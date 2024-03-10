using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
    public static UIController Instance;
    [SerializeField] private VisualTreeAsset statRowPrefab;
    [SerializeField] private GameArbiter gameArbiter;
    [SerializeField] private GunCommunication physicalGun;
    [SerializeField] private GyroscopeCommuincation gyroscope;
    [SerializeField] private Camera scopeCamera;
    [SerializeField] private Vector2 scopeFOVRange = new(40, 120);
    [SerializeField] private float scopeDefaultFOV = 60;
    [SerializeField] private UIDocument document;

    List<StatRecord> statRecords = new();

    private float gameTime = 60f;
    public float GameTime => gameTime;

    private VisualElement RootVisualElement => document.rootVisualElement;

    private Button menuButton;
    private Button lockOutRealGun;
    private Button startButton;
    
    private Button calibrateButton;

    private Button statsButton;
    private Button returnToGameButton;
    private Button resetGameButton;
    private Button closeGameButton;

    private Button closeStatsButton;
    private Button statsMenuButton;

    private Toggle limitGameTime;
    private Toggle enableRealGun;

    private Slider zoomSlider;

    private Slider gameTimeSlider;
    private Slider targetIntervalSlider;

    private Slider leftSlider;
    private Slider rightSlider;
    private Slider topSlider;
    private Slider bottomSlider;

    private Label timeOut;
    private Label scoreOut;

    public float DisplayedTime
    {
        set
        {
            TimeSpan time = TimeSpan.FromSeconds(value);
            timeOut.text = string.Format("{0}:{1}.{2}", time.Minutes.ToString("D2"), time.Seconds.ToString("D2"), time.Milliseconds.ToString("D3"));
        }
    }

    public string DisplayedScore { set => scoreOut.text = value; }

    private VisualElement leftCover;
    private VisualElement rightCover;
    private VisualElement topCover;
    private VisualElement bottomCover;

    private VisualElement gameUI;

    private VisualElement menuUI;
    private VisualElement settingsUI;
    private VisualElement statsUI;

    private VisualElement statsScrollViewContentContainer;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
        QueryDocument();
        SetCallbacks();
        SetScopeBorders();
        DefaultScope();
        ShowGameUI();
        gameTimeSlider.SetValueWithoutNotify(gameTime);
        OnGameTimeSliderChanged(gameTime);

        gameTimeSlider.SetValueWithoutNotify(gameArbiter.newStaticTargetTime);
        OnDiffcultyChanged(gameArbiter.newStaticTargetTime);
        Instance = this;
    }

    private void QueryDocument()
    {
        menuButton = RootVisualElement.Q<Button>("menuButton");
        lockOutRealGun = RootVisualElement.Q<Button>("lockoutFirebutton");
        startButton = RootVisualElement.Q<Button>("StartButton");

        calibrateButton = RootVisualElement.Q<Button>("CalibrateButton");

        statsButton = RootVisualElement.Q<Button>("GameStatsButton");
        returnToGameButton = RootVisualElement.Q<Button>("ReturnToGameButton");
        resetGameButton = RootVisualElement.Q<Button>("ResetGameButton");
        closeGameButton = RootVisualElement.Q<Button>("CloseGameButton");
        
        closeStatsButton = RootVisualElement.Q<Button>("CloseStats");
        statsMenuButton = RootVisualElement.Q<Button>("StatsMainMenu");

        limitGameTime = RootVisualElement.Q<Toggle>("LimitGameTime");
        enableRealGun = RootVisualElement.Q<Toggle>("EnablePhysicalFiring");

        zoomSlider = RootVisualElement.Q<Slider>("zoomSlider");

        gameTimeSlider = RootVisualElement.Q<Slider>("GameTimeSlider");
        targetIntervalSlider = RootVisualElement.Q<Slider>("DifficultySlider");

        leftSlider = RootVisualElement.Q<Slider>("LeftCover");
        rightSlider = RootVisualElement.Q<Slider>("RightCover");
        topSlider = RootVisualElement.Q<Slider>("TopCover");
        bottomSlider = RootVisualElement.Q<Slider>("BottomCover");

        timeOut = RootVisualElement.Q<Label>("TimeLeftOut");
        scoreOut = RootVisualElement.Q<Label>("ScoreOut");

        leftCover = RootVisualElement.Q("leftBlack");
        rightCover = RootVisualElement.Q("rightBlack");
        topCover = RootVisualElement.Q("topBlack");
        bottomCover = RootVisualElement.Q("bottomBlack");

        menuUI = RootVisualElement.Q("MenuUI");
        settingsUI = RootVisualElement.Q("SettingsContainer");
        gameUI = RootVisualElement.Q("GameUI");
        statsUI = RootVisualElement.Q("StatsContainer");
        statsScrollViewContentContainer = statsUI.Q("StatTable");
    }

    private void SetCallbacks()
    {
        menuButton.RegisterCallback<NavigationSubmitEvent>(ev => ShowSettingsUI());
        menuButton.RegisterCallback<ClickEvent>(ev => ShowSettingsUI());

        lockOutRealGun.RegisterCallback<NavigationSubmitEvent>(ev => SetFiringMode(false));
        lockOutRealGun.RegisterCallback<ClickEvent>(ev => SetFiringMode(false));

        startButton.RegisterCallback<NavigationSubmitEvent>(ev => OnStartButton());
        startButton.RegisterCallback<ClickEvent>(ev => OnStartButton());

        limitGameTime.RegisterValueChangedCallback(ev=> OnLimitGameTimeChange(ev.newValue));

        enableRealGun.RegisterValueChangedCallback(ev => SetFiringMode(ev.newValue));


        calibrateButton.RegisterCallback<NavigationSubmitEvent>(ev => CalibrateCompass());
        calibrateButton.RegisterCallback<ClickEvent>(ev => CalibrateCompass());


        statsButton.RegisterCallback<NavigationSubmitEvent>(ev => ShowStats());
        statsButton.RegisterCallback<ClickEvent>(ev => ShowStats());


        closeStatsButton.RegisterCallback<NavigationSubmitEvent>(ev => ShowGameUI());
        closeStatsButton.RegisterCallback<ClickEvent>(ev => ShowGameUI());

        statsMenuButton.RegisterCallback<NavigationSubmitEvent>(ev => ShowSettingsUI());
        statsMenuButton.RegisterCallback<ClickEvent>(ev => ShowSettingsUI());

        returnToGameButton.RegisterCallback<NavigationSubmitEvent>(ev=>ShowGameUI());
        returnToGameButton.RegisterCallback<ClickEvent>(ev => ShowGameUI());

        resetGameButton.RegisterCallback<NavigationSubmitEvent>(ev => ResetGame());
        resetGameButton.RegisterCallback<ClickEvent>(ev => ResetGame());

        closeGameButton.RegisterCallback<NavigationSubmitEvent>(ev => CloseGame());
        closeGameButton.RegisterCallback<ClickEvent>(ev => CloseGame());

        gameTimeSlider.RegisterValueChangedCallback(ev => OnGameTimeSliderChanged(ev.newValue));
        targetIntervalSlider.RegisterValueChangedCallback(ev=> OnDiffcultyChanged(ev.newValue));

        leftSlider.RegisterValueChangedCallback(ev => OnScopeSliderChangedX(leftCover, ev.newValue));
        rightSlider.RegisterValueChangedCallback(ev => OnScopeSliderChangedX(rightCover, ev.newValue));
        topSlider.RegisterValueChangedCallback(ev => OnScopeSliderChangedY(topCover, ev.newValue));
        bottomSlider.RegisterValueChangedCallback(ev => OnScopeSliderChangedY(bottomCover, ev.newValue));

        zoomSlider.RegisterValueChangedCallback(ev => OnScopeSliderChanged(ev.newValue));
    }

    #region Callbacks
    #region In Game UI
    private void ShowSettingsUI()
    {
        gameUI.style.display = DisplayStyle.None;
        statsUI.style.display = DisplayStyle.None;
        menuUI.style.display = DisplayStyle.Flex;
        settingsUI.style.display = DisplayStyle.Flex;
    }

    private void OnScopeSliderChanged(float newValue)
    {
        float newFOV = Mathf.Lerp(scopeFOVRange.x, scopeFOVRange.y, newValue/10);
        scopeCamera.fieldOfView = newFOV;
    }

    private void OnStartButton()
    {
        startButton.style.display = DisplayStyle.None;
        gameArbiter.RestartGame();
    }

    #endregion
    #region Settings Menu
    public void ResetGame()
    {
        gameArbiter.ExternalEndGame();
        DefaultScope();
        ShowGameUI();
        DisplayedTime = gameTime;
        DisplayedScore = 0.ToString("D3");
        startButton.style.display = DisplayStyle.Flex;
    }

    private void ShowGameUI()
    {
        SaveScopeBorders();
        gameUI.style.display = DisplayStyle.Flex;
        menuUI.style.display = DisplayStyle.None;
        statsUI.style.display = DisplayStyle.None;
        settingsUI.style.display = DisplayStyle.None;
    }

    private void CloseGame()
    {
        SaveScopeBorders();
        Application.Quit();
    }

    private void CalibrateCompass()
    {
        gyroscope.BeginReceiveGyroCalibration();
    }

    private void OnScopeSliderChangedX(VisualElement targetGraphic, float newValue)
    {
        Length legnth = targetGraphic.style.width.value;
        legnth.value = newValue;
        legnth.unit = LengthUnit.Percent;
        targetGraphic.style.width = legnth;
    }

    private void OnScopeSliderChangedY(VisualElement targetGraphic, float newValue)
    {
        Length legnth = targetGraphic.style.height.value;
        legnth.value = newValue;
        legnth.unit = LengthUnit.Percent;
        targetGraphic.style.height = legnth;
    }

    private void OnLimitGameTimeChange(bool newValue)
    {
        if(!newValue)
        {
            gameTime = float.MaxValue;
            gameTimeSlider.style.display = DisplayStyle.None;
        }
        else
        {
            gameTimeSlider.style.display = DisplayStyle.Flex;
            OnGameTimeSliderChanged(gameTimeSlider.value);
        }
    }

    private void OnGameTimeSliderChanged(float newValue)
    {
        gameTimeSlider.label = string.Format("Game Time: {0}s", newValue);
        DisplayedTime = gameTime = newValue;
    }

    private void OnDiffcultyChanged(float newValue)
    {
        gameArbiter.newRaisableTargetTime = gameArbiter.newStaticTargetTime = newValue;
        targetIntervalSlider.label = string.Format("Target Spawn\nInterval: {0}s", newValue);
    }

    #endregion
    #endregion

    #region Helper Methods
    #region Scope Borders
    private void SetScopeBorders()
    {
        Vector4 borders = PersistantOptions.instance.gyroCalibrationData.scopeBorders;

        OnScopeSliderChangedX(leftCover, borders.x);
        OnScopeSliderChangedX(rightCover, borders.y);
        OnScopeSliderChangedY(topCover, borders.z);
        OnScopeSliderChangedY(bottomCover, borders.w);

        leftSlider.SetValueWithoutNotify(borders.x);
        rightSlider.SetValueWithoutNotify(borders.y);
        topSlider.SetValueWithoutNotify(borders.z);
        bottomSlider.SetValueWithoutNotify(borders.w);
    }

    private void SaveScopeBorders()
    {
        Vector4 borders = new(leftSlider.value, rightSlider.value, topSlider.value, bottomSlider.value);
        PersistantOptions.instance.gyroCalibrationData.scopeBorders = borders;
    }
    #endregion

    #region Calibration
    public void AllowCalibration(bool allowed)
    {
        calibrateButton.SetEnabled(allowed);
        startButton.SetEnabled(allowed);
        resetGameButton.SetEnabled(allowed) ;
    }
    #endregion

    #region FiringMode
    private void SetFiringMode(bool allowed)
    {
        lockOutRealGun.SetEnabled(allowed);
        physicalGun.SetNerfGunEmulationMode(allowed);
    }
    public void UpdateFiringMode(bool allowed)
    {
        enableRealGun.SetValueWithoutNotify(allowed);
        lockOutRealGun.SetEnabled(allowed);
    }
    #endregion

    private void DefaultScope()
    {
        scopeCamera.fieldOfView = scopeDefaultFOV;
        zoomSlider.SetValueWithoutNotify(Mathf.InverseLerp(scopeFOVRange.x, scopeFOVRange.y, scopeDefaultFOV*10));
    }

    public void ShowStats()
    {
        gameUI.style.display = DisplayStyle.None;
        statsUI.style.display = DisplayStyle.Flex;
        menuUI.style.display = DisplayStyle.Flex;
        settingsUI.style.display = DisplayStyle.None;

        statRecords.Sort();
        statsScrollViewContentContainer.Clear();

        StatRow titleRow = new(statRowPrefab.Instantiate());
        statsScrollViewContentContainer.Add(titleRow.root);
        for (int i = 0; i < statRecords.Count; i++)
        {
            StatRow row = new(statRowPrefab.Instantiate());
            row.SetStats(statRecords[i]);
            statsScrollViewContentContainer.Add(row.root);
        }
    }

    public void ShowStats(StatRecord newStat)
    {
        statRecords.Add(newStat);
        ShowStats();
    }

    #endregion
}

public class StatRecord : IComparable<StatRecord>
{
    public float scorePerSecond;
    public int totalScore;
    public float time;
    public float accuracy;
    public int hits;
    public int shots;
    public float averageTargetUpTime;

    public int CompareTo(StatRecord other)
    {
        return scorePerSecond.CompareTo(other.scorePerSecond);
    }
}

public class StatRow
{
    public TemplateContainer root;
    public Label scorePerSecond;
    public Label totalScore;
    public Label time;
    public Label accuracy;
    public Label hits;
    public Label shots;
    public Label averageTargetUpTime;

    public StatRow(TemplateContainer template)
    {
        root = template;
        scorePerSecond = root.Q<Label>("SPS");
        totalScore = root.Q<Label>("Score");
        time = root.Q<Label>("Time");
        accuracy = root.Q<Label>("Accuracy");
        hits = root.Q<Label>("Hits");
        shots = root.Q<Label>("Shots");
        averageTargetUpTime = root.Q<Label>("ATUT");
    }

    public void SetStats(StatRecord record)
    {
        scorePerSecond.text = record.scorePerSecond.ToString("0.00");
        totalScore.text = record.totalScore.ToString("D3");
        time.text = record.time.ToString("0.00");
        accuracy.text = record.accuracy.ToString("0.00");
        hits.text = record.hits.ToString("D3");
        shots.text = record.shots.ToString("D3");
        averageTargetUpTime.text = record.averageTargetUpTime.ToString("0.00");
    }
}