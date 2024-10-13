using Godot;

[Tool]
public partial class DevConsoleContainer : Control
{
	const string THEME_PATH = "res://addons/vekDevConsole/consoleTheme.tres";
	public float AnimLen = 0.25f;
	public float ScreenDiv = 2.25f;

	public int FontSize = 24;
	
	Panel _consolePanel;
	LineEdit _consoleInput;
	RichTextLabel _consoleLog;
	Label _consoleInfo;
	VBoxContainer _consoleContainer;

	bool _showConsole = false;
	bool _hasInterpolation = false;

	ulong _prevFrame = 0;
	float _unscaledDelta = 0;
	
	// Called when the node enters the scene tree for the first time.
	public override void _EnterTree()
	{
		Theme = GD.Load<Theme>(THEME_PATH);
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
		ClipContents = true;

		// Background
		_consolePanel = new();
		_consolePanel.Size = new Vector2(Size.X, Size.Y / ScreenDiv);
		_consolePanel.MouseFilter = MouseFilterEnum.Pass;
		
		AddChild(_consolePanel);

		// Layout
		_consoleContainer = new();
		_consoleContainer.SetAnchorsPreset(LayoutPreset.FullRect);
		_consoleContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		_consoleContainer.Alignment = BoxContainer.AlignmentMode.End;

		_consolePanel.AddChild(_consoleContainer);

		// Log
		_consoleLog = new RichTextLabel();
		_consoleLog.SetAnchorsPreset(LayoutPreset.FullRect);
		_consoleLog.ScrollFollowing = true;
		_consoleLog.SelectionEnabled = true;
		_consoleContainer.AddChild(_consoleLog);
		
		// Game/Engine Info
		_consoleInfo = new();
		_consoleInfo.Text = 
			(string)ProjectSettings.GetSetting("application/config/name") + " " +
			(string)ProjectSettings.GetSetting("application/config/version") +
			" // Godot Engine " + (string)Engine.GetVersionInfo()["string"];
		_consoleInfo.HorizontalAlignment = HorizontalAlignment.Right;
		_consoleInfo.VerticalAlignment = VerticalAlignment.Center;

		_consoleContainer.AddChild(_consoleInfo);

		// Input
		_consoleInput = new();
		_consoleInput.PlaceholderText = "Enter Command";
		_consoleInput.MouseFilter = MouseFilterEnum.Stop;
		_consoleInput.ContextMenuEnabled = false;
		
		_consoleInput.TextSubmitted += OnTextSubmitted;

		_consoleContainer.AddChild(_consoleInput);

		ApplyFontSize();

	}

    public override void _Ready()
    {
		// Hides console at start.
		_consolePanel.Position = new(0, Size.Y / -ScreenDiv);

		if(DevConsole._consoleContainer != null)
			GD.PushError("More than one console exists. Please ensure only one console exists at a time.");
		else
		{
			DevConsole._consoleContainer = this;
			DevConsole.ConsoleSetTask?.SetResult(true);
		}
		
		CallDeferred(nameof(UpdateLogSize));
		
		// Add commands.
		DevConsole.AddCommand("con_fontSize", new(){
			Action = ChangeFontSize,
			ReadAction = GetFontSize,
			Description = "Change the console's font size."
		});

		DevConsole.AddCommand("con_openLength", new(){
			Action = SetAnimLength,
			ReadAction = GetAnimLength,
			Description = "Change the length of the console's opening animation. The length is arbitrary."
		});

		DevConsole.AddCommand("con_openSmooth", new(){
			Action = SetInterpolated,
			ReadAction = GetInterpolated,
			Description = "Change whether or not the opening animation of the console is linearly interpolated."
		});
		DevConsole.AddCommand("clear", new(){
			Action = ClearConsole,
			Description = "Clear the console's log."
		});
    }

    public override void _Process(double delta)
    {
		// If you change the time scale the standard delta here will be affected. 
		// This means the opening animation simply won't work when, for instance, paused.
		// To get around this, we simply calculate our own delta.
		_unscaledDelta = (int)(Time.GetTicksMsec() - _prevFrame) / 1000f;
		_prevFrame = Time.GetTicksMsec();

		if (_hasInterpolation)
		{
			// Smooth open
			if(_showConsole && _consolePanel.Position.Y < 0)
				_consolePanel.Position = new(0, Mathf.Lerp(_consolePanel.Position.Y, 0, 
				 Size.Y / (ScreenDiv * 24) * (float)_unscaledDelta * AnimLen));
			
			if (!_showConsole && _consolePanel.Position.Y > Size.Y / -ScreenDiv)
				_consolePanel.Position = new(0, Mathf.Lerp(_consolePanel.Position.Y, Size.Y / -ScreenDiv, 
				 Size.Y / (ScreenDiv * 24) * (float)_unscaledDelta * AnimLen));
		}
		else
		{
			// Not so smooth open
			if(_showConsole && _consolePanel.Position.Y < 0)
				_consolePanel.Position = new(0, 
				 _consolePanel.Position.Y + Size.Y / ScreenDiv * (float)_unscaledDelta / AnimLen);
			
			if (!_showConsole && _consolePanel.Position.Y > Size.Y / -ScreenDiv)
				_consolePanel.Position = new(0, 
				 _consolePanel.Position.Y - Size.Y / ScreenDiv * (float)_unscaledDelta / AnimLen);
		}

		_consolePanel.Position = new(0, Mathf.Clamp(_consolePanel.Position.Y, -960,0));
	}

    public override void _Input(InputEvent @event)
    {
		if(Input.IsActionJustPressed("dev_console"))
		{
			_showConsole = !_showConsole;
		}
    }

	public void ApplyFontSize()
	{
		_consoleInfo.AddThemeFontSizeOverride("font_size", FontSize);
		_consoleInput.AddThemeFontSizeOverride("font_size", FontSize);
		_consoleLog.AddThemeFontSizeOverride("normal_font_size", FontSize);
		_consoleLog.AddThemeFontSizeOverride("mono_font_size", FontSize);
		_consoleLog.AddThemeFontSizeOverride("bold_font_size", FontSize);
		_consoleLog.AddThemeFontSizeOverride("italics_font_size", FontSize);
		_consoleLog.AddThemeFontSizeOverride("bold_italics_font_size", FontSize);
	}

	public void ChangeFontSize(int size)
	{
		FontSize = size;
		ApplyFontSize();
		_consoleLog.CustomMinimumSize = new(Size.X, 0);
		CallDeferred(nameof(DoubleDeferHack));
	}

	public void GetFontSize()
	{
		DevConsole.Print($"Font Size: {FontSize}");
	}

	private void UpdateLogSize()
	{
		_consoleLog.CustomMinimumSize = new(Size.X, _consoleLog.Position.Y);
	}

	public void Print(string text)
	{
		_consoleLog.AppendText("\n" + text);
	}

	private void OnTextSubmitted(string text)
	{
		DevConsole.Print("> " + text);
		_consoleInput.Clear();

		DevConsole.SubmitCommand(text);
	}

	private void DoubleDeferHack()
	{
		CallDeferred(nameof(UpdateLogSize));
	}

	private void SetAnimLength(float length)
	{
		length = Mathf.Clamp(length, 0.01f, 10f);
		AnimLen = length;
	}

	private void GetAnimLength()
	{
		DevConsole.Print($"Open Anim Length (arbitrary): {AnimLen}");
	}

	private void SetInterpolated(bool value)
	{
		_hasInterpolation = value;
	}
	private void GetInterpolated()
	{
		DevConsole.Print($"Opens smoothly: {_hasInterpolation}");
	}

	private void ClearConsole()
	{
		_consoleLog.Text = "";
	}
}
