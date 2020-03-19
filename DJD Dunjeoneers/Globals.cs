using Godot;
using System;

public class Globals : Node{
    [Signal] public delegate float TimeScaleChanged();
    [Signal] public delegate float MusicDbChanged();
    [Signal] public delegate float PlayerSfxDbChanged();
    [Signal] public delegate float EnemySfxDbChanged();
    [Signal] public delegate bool EnemySfxPaused();
    [Signal] public delegate bool PlayerSfxPaused();
    [Signal] public delegate bool MusicPaused();

    public Node CurrentScene { get; set; }
    public float minDb = -15f;
    private float _timeScale = 1f;
    private float _musicDb = 0f;
    private float _playerSfxDb = 0f;
    private float _enemySfxDb = 0f;
    
    public float TimeScale { 
        get { return _timeScale; }
        set {
            _timeScale = value;
            EmitSignal("TimeScaleChanged", value);
        }
    }

    public float MusicDb { 
        get { return _musicDb; }
        set {
            _musicDb = value;
            EmitSignal("MusicPaused", _musicDb == minDb);
            
            EmitSignal("MusicDbChanged", value);
        }
    }

    public float PlayerSfxDb { 
        get { return _playerSfxDb; }
        set {
            _playerSfxDb = value;
            EmitSignal("PlayerSfxPaused", _playerSfxDb == minDb);
            EmitSignal("PlayerSfxDbChanged", value);
        }
    }

    public float EnemySfxDb { 
        get { return _enemySfxDb; }
        set {
            _enemySfxDb = value;
            EmitSignal("EnemySfxPaused", _enemySfxDb == minDb);
            EmitSignal("EnemySfxDbChanged", value);
        }
    }

    public override void _Ready(){
        Viewport root = GetTree().Root;
        CurrentScene = root.GetChild(root.GetChildCount() - 1);
    }
}
