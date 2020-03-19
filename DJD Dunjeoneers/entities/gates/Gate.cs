using Godot;
using System;
using System.Collections.Generic;

public class Gate : Node2D{

    [Signal] public delegate Gate Complete();
    
    private AnimatedSprite _sprite = new AnimatedSprite();
    private Tween _tween = new Tween();
    private List<(Type, int)> _waveDefinition;
    private List<Entity> _waveEntities;
    private List<Entity> _toSpawn = new List<Entity>();
    private Timer _spawnTimer = new Timer();

    private Light2D _light = new Light2D();
    private int _nextSpawnRequireTotal = 0;
    private int _nextSpawnRequireCurrent = 0;
    private Random _rng = new Random();
    private Floor _floor;

    private Gate(){}

    public Gate(Floor floor, List<(Type, int)> waveDefinition){
        _floor = floor;
        _waveDefinition = waveDefinition;
        
        _sprite.Frames = ResourceLoader.Load("res://entities/gates/Gate.tres") as SpriteFrames;

        Texture gradient = ResourceLoader.Load("res://assets/gradients/radial.png") as Texture;
        _light.Texture = gradient;
        _light.Scale = new Vector2(.1f, .1f);

        AddChild(_sprite);
        AddChild(_light);
        AddChild(_tween);
        AddChild(_spawnTimer);
    }

    public override void _Ready(){
        _sprite.Play("default");
        _tween.InterpolateProperty(_light, "color", new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, .4f), 2f);
        _tween.InterpolateCallback(this, 2f, "SetupWave");
        _tween.Start();

        _spawnTimer.WaitTime = .2f;
        _spawnTimer.Connect("timeout", this, "CheckToSpawn");
        _spawnTimer.Start();
    }

    public void PopulateWaveEntities(){
        GD.Print("spawning new enemies");
        _waveEntities = new List<Entity>();
        foreach ((Type enemyType, int count) in _waveDefinition){
            for (int i = 0; i < count; i++)
                _waveEntities.Add(Activator.CreateInstance(enemyType, Position, _floor.areaStart, _floor.areaEnd) as Entity);
        }
    }

    public void SetupWave(){
        PopulateWaveEntities();
        var count = _waveEntities.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i) {
            var r = _rng.Next(i + 1);
            var tmp = _waveEntities[i];
            _waveEntities[i] = _waveEntities[r];
            _waveEntities[r] = tmp;
        }
        foreach(Entity entity in _waveEntities)entity.Connect("Dead", this, "OnEntityDead");
        ContinueWave();
    }

    public void ContinueWave(){
        _nextSpawnRequireTotal = 0;
        if (_waveEntities.Count == 0){
            Finished();
            return;
        }
        var numEnemiesToSpawn = _rng.Next(Mathf.Min(_waveEntities.Count, 5), Mathf.Min(_waveEntities.Count, 10));
        for (var j = numEnemiesToSpawn - 1; j >= 0; j--){
            _nextSpawnRequireTotal += _waveEntities[j].DeathValue;
            _toSpawn.Add(_waveEntities[j]);
            _waveEntities.RemoveAt(j);
        }
    }

    private void CheckToSpawn(){
        if (_toSpawn.Count > 0){
            GetTree().Root.AddChild(_toSpawn[0]);
            _toSpawn.RemoveAt(0);
        }
    }

    private void Finished(){
        _tween.InterpolateProperty(_light, "color", _light.Color, new Color(1f, 1f, 1f, .0f), 2f);
        _tween.InterpolateCallback(this, 2f, "queue_free");
        _tween.Start();
        EmitSignal("Complete", this);
    }

    private void OnEntityDead(int value){
        _nextSpawnRequireCurrent += value;
        if (_nextSpawnRequireCurrent >= _nextSpawnRequireTotal){
            _nextSpawnRequireCurrent = 0;
            ContinueWave();
        }
    }

    public override void _Process(float delta){

    }
}
