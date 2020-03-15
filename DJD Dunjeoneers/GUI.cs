using Godot;
using System;

public class GUI : Control{
    private enum EGuiState {
        MAIN,
        OPTIONS,
        HIDDEN
    }

    private EGuiState state = EGuiState.MAIN;
    private VBoxContainer titleScreen;
    private VBoxContainer mainMenu;
    private TextureButton play;
    private TextureButton menu;
    private TextureButton exit;
    private VBoxContainer optionsMenu;
    private HSlider bgm;
    private HSlider playerSfx;
    private HSlider enemySfx;
    private Globals globals;

    public override void _Ready(){
        globals = GetTree().Root.GetNode("Globals") as Globals;
        titleScreen = GetNode<VBoxContainer>("Title Screen");
        mainMenu = titleScreen.GetNode<VBoxContainer>("Main Menu");
        play = mainMenu.GetNode<TextureButton>("Play");
        menu = mainMenu.GetNode<TextureButton>("Menu");
        exit = mainMenu.GetNode<TextureButton>("Exit");

        optionsMenu = titleScreen.GetNode<VBoxContainer>("Options Menu");
        bgm = optionsMenu.GetNode<HBoxContainer>("Music").GetNode<HSlider>("Slider");
        playerSfx = optionsMenu.GetNode<HBoxContainer>("Player SFX").GetNode<HSlider>("Slider");
        enemySfx = optionsMenu.GetNode<HBoxContainer>("Enemy SFX").GetNode<HSlider>("Slider");
        optionsMenu.Hide();
        
        play.Connect("pressed", this, "_OnPlayPressed");
        menu.Connect("pressed", this, "_OnMenuPressed");
        exit.Connect("pressed", this, "_OnExitPressed");

        bgm.Connect("value_changed", this, "_OnBgmDbChanged");
        playerSfx.Connect("value_changed", this, "_OnPlayerSfxDbChanged");
        enemySfx.Connect("value_changed", this, "_OnEnemySfxDbChanged");

        bgm.Value = globals.MusicDb;
        playerSfx.Value = globals.PlayerSfxDb;
        enemySfx.Value = globals.EnemySfxDb;

        bgm.MaxValue = 15f;
        bgm.Value = globals.MusicDb;
        bgm.MinValue = globals.minDb;
        playerSfx.MaxValue = 15;
        playerSfx.Value = globals.PlayerSfxDb;
        playerSfx.MinValue = globals.minDb;
        enemySfx.MaxValue = 15f;
        enemySfx.Value = globals.EnemySfxDb;
        enemySfx.MinValue = globals.minDb;
    }

    public void _OnPlayPressed(){
        Engine.TimeScale = 1;
        state = EGuiState.HIDDEN;
        Hide();
        GetTree().Root.GetNode<Game>("Game").Reset();
    }

    public void _OnBgmDbChanged(float value){
        globals.MusicDb = value;
    }

    public void _OnPlayerSfxDbChanged(float value){
        globals.PlayerSfxDb = value;
    }

    public void _OnEnemySfxDbChanged(float value){
        globals.EnemySfxDb = value;
    }

    public void _OnMenuPressed(){
        mainMenu.Hide();
        optionsMenu.Show();
        state = EGuiState.OPTIONS;
    }

    public void _OnExitPressed(){
        GetTree().Quit();
    }

    public override void _Input(InputEvent @event){
        if (Input.IsActionJustPressed("ui_cancel")) Back();
    }

    public void Back(){
        switch (state){
            case EGuiState.HIDDEN:
                Show();
                if (GetTree().GetNodesInGroup("Players").Count > 0)
                    Engine.TimeScale = 0;
                else
                    Engine.TimeScale = 1;
                state = EGuiState.MAIN;
                break;
            case EGuiState.MAIN:
                Hide();
                Engine.TimeScale = 1;
                state = EGuiState.HIDDEN;
                break;
            case EGuiState.OPTIONS:
                optionsMenu.Hide();
                mainMenu.Show();
                state = EGuiState.MAIN;
                break;
            default:
                break;
        }
    }
}
