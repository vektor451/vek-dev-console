using System.Collections.Generic;
using Godot;

[Tool]
public partial class DevConsoleContainer : Control
{
	const string THEME_PATH = "res://addons/vekDevConsole/consoleTheme.tres";
	const string AUTOCOMPLETE_OVERRIDE_PATH = "res://addons/vekDevConsole/autoCompleteStyleBox.tres";
	public float AnimLen = 0.25f;
	public int FontSize = 24;
	
	Panel _consolePanel;
	LineEdit _consoleInput;
	RichTextLabel _consoleLog;
	Label _consoleInfo;
	Label _autoCompleteLabel;
	VBoxContainer _consoleContainer;

	bool _showConsole = false;
	bool _hasInterpolation = false;

	ulong _prevFrame = 0;
	float _unscaledDelta = 0;

	List<string> _autoCompleteStrings; 
	int _autoCompleteIndex = 0;
	bool _iterateAutoComplete = false;

	string _colorPrintString = "Red: 255, Green: 255, Blue: 255";
	int _consoleHeight = 960;
	float _bgOpacity = 0.8f;
	// Called when the node enters the scene tree for the first time.
	public override void _EnterTree()
	{
		Theme = GD.Load<Theme>(THEME_PATH);
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
		ClipContents = true;

		// Background
		_consolePanel = new();
		_consolePanel.Size = new Vector2(Size.X, _consoleHeight);
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
		_consoleInput.TextChanged += OnTextChanged;

		_consoleContainer.AddChild(_consoleInput);

		// Autocomplete
		_autoCompleteLabel = new();
		_autoCompleteLabel.AddThemeStyleboxOverride("normal", GD.Load<StyleBox>(AUTOCOMPLETE_OVERRIDE_PATH));
		_consoleInput.AddChild(_autoCompleteLabel);
		//_autoCompleteLabel.Visible = false;
		_autoCompleteLabel.Position = new(0,32);

		ApplyFontSize();
	}

	public override void _Ready()
	{
		// Hides console at start.
		_consolePanel.Position = new(0, Size.Y / -_consoleHeight);

		if(DevConsole._consoleContainer != null)
			GD.PushError("More than one console exists. Please ensure only one console exists at a time.");
		else
		{
			DevConsole._consoleContainer = this;
			DevConsole.ConsoleSetTask?.SetResult(true);
		}
		
		UpdateConsoleHeight(_consoleHeight);
		ChangeFontSize(FontSize);
		
		_consolePanel.GetThemeStylebox("normal").Set("bg_color", new Color(0, 0, 0, _bgOpacity));
		
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

		DevConsole.AddCommand("con_accentColor", new(){
			Action = UpdateAccentColor,
			ReadAction = GetAccentColor,
			Description = "Change the console's accent color (the borders on the input, for instance).",
		});

		DevConsole.AddCommand("con_height", new(){
			Action = UpdateConsoleHeight,
			ReadAction = GetConsoleHeight,
			Description = "Change the console's height to a pixel value. May not dirrectly correlate to screen resolution.",
		});

		DevConsole.AddCommand("con_bgOpacity", new(){
			Action = UpdateBGOpacity,
			ReadAction = GetBGOpacity,
			Description = "Change the opacity of the console's background to a value between 0 and 1.",
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
				 _consoleHeight * 24 * _unscaledDelta * AnimLen));
			
			if (!_showConsole && _consolePanel.Position.Y > -_consoleHeight)
				_consolePanel.Position = new(0, Mathf.Lerp(_consolePanel.Position.Y, -_consoleHeight, 
				 _consoleHeight * (float)_unscaledDelta * AnimLen));
		}
		else
		{
			// Not so smooth open
			if(_showConsole && _consolePanel.Position.Y < 0)
				_consolePanel.Position = new(0, 
				 _consolePanel.Position.Y + _consoleHeight * _unscaledDelta / AnimLen);
			
			if (!_showConsole && _consolePanel.Position.Y > -_consoleHeight)
				_consolePanel.Position = new(0, 
				 _consolePanel.Position.Y - _consoleHeight * _unscaledDelta / AnimLen);
		}

		_consolePanel.Position = new(0, Mathf.Clamp(_consolePanel.Position.Y, -_consoleHeight, 0));
	}

	public override void _Input(InputEvent @event)
	{
		if(Input.IsActionJustPressed("dev_console"))
		{
			_showConsole = !_showConsole;

			if(_showConsole)
			{
				CallDeferred(nameof(EnableInput));
				_consoleInput.GrabFocus();
				_autoCompleteLabel.Visible = false;
			}
			else
			{
				_consoleInput.Editable = false;
				_autoCompleteLabel.Visible = _autoCompleteLabel.Text != "";
			}
		}
		if(Input.IsActionJustPressed("ui_text_indent") && _autoCompleteStrings.Count > 0)
		{
			if (_iterateAutoComplete)
			{
				_autoCompleteIndex++;
				if(_autoCompleteIndex == _autoCompleteStrings.Count)
					_autoCompleteIndex = 0;
			}
			
			_consoleInput.Text = $"{_autoCompleteStrings[_autoCompleteIndex]} ";
			_consoleInput.CaretColumn = _consoleInput.Text.Length;
			CallDeferred(nameof(AutocompleteDefer));
		}
	}

	public void OnTextChanged(string text)
	{
		List<string> commands = DevConsole.SuggestCommands(text);
		_autoCompleteLabel.Text = "";

		foreach (string command in commands)
		{
			_autoCompleteLabel.Text += $"{command}\n";
		}

		if (_autoCompleteLabel.Text == "")
			_autoCompleteLabel.Visible = false;
		else
			_autoCompleteLabel.Visible = true;

		_autoCompleteStrings = commands;
		_iterateAutoComplete = false;
		_autoCompleteIndex = 0;
	}

	public void ApplyFontSize()
	{
		_consoleInfo.AddThemeFontSizeOverride("font_size", FontSize);
		_consoleInput.AddThemeFontSizeOverride("font_size", FontSize);
		_autoCompleteLabel.AddThemeFontSizeOverride("font_size", FontSize);
		_consoleLog.AddThemeFontSizeOverride("normal_font_size", FontSize);
		_consoleLog.AddThemeFontSizeOverride("mono_font_size", FontSize);
		_consoleLog.AddThemeFontSizeOverride("bold_font_size", FontSize);
		_consoleLog.AddThemeFontSizeOverride("italics_font_size", FontSize);
		_consoleLog.AddThemeFontSizeOverride("bold_italics_font_size", FontSize);
	}

	public void ChangeFontSize(int size)
	{
		size = Mathf.Clamp(size, 8, 72);
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
		_autoCompleteLabel.Position = new(0,_consoleInput.Size.Y);
	}

	private void UpdateConsoleHeight(int height)
	{
		height = Mathf.Clamp(height, 192, (int)Size.Y - 192);
		
		_consoleHeight = height;
		_consolePanel.Size = new Vector2(Size.X, _consoleHeight);
		ChangeFontSize(FontSize);
	}

	private void GetConsoleHeight()
	{
		DevConsole.Print($"Height: {_consoleHeight}");
	}

	public void Print(string text)
	{
		_consoleLog.AppendText("\n" + text);
	}

	private void OnTextSubmitted(string text)
	{
		DevConsole.Print($"[color=Silver]> {text}[/color]");
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

	private void EnableInput()
	{
		_consoleInput.Editable = true;
	}

	private void AutocompleteDefer()
	{
		_consoleInput.GrabFocus();
		_iterateAutoComplete = true;
	}

	private void UpdateAccentColor(int red, int green, int blue)
	{
		Color accent = new(red/255f, green/255f, blue/255f);
		_colorPrintString = $"Red: {red}, Green: {green}, Blue: {blue}"; // too hacky imo
		_consolePanel.GetThemeStylebox("normal").Set("border_color", accent);
		_consoleInput.GetThemeStylebox("normal").Set("border_color", accent);
	}

	private void GetAccentColor()
	{
		DevConsole.Print($"Accent Color: {_colorPrintString}");
	}

	private void UpdateBGOpacity(float opacity)
	{
		_bgOpacity = Mathf.Clamp(opacity, 0f, 1f);
		_consolePanel.GetThemeStylebox("panel").Set("bg_color", new Color(0, 0, 0, opacity));
		_autoCompleteLabel.GetThemeStylebox("normal").Set("bg_color", new Color(0, 0, 0, opacity));
	}

	private void GetBGOpacity()
	{
		Print($"Background Opacity: {_bgOpacity}");
	}
}
