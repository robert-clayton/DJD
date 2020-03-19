using Godot;
using System;
using System.Collections.Generic;

public class Gate : Node{
    
    private AnimatedSprite _sprite;
    private Tween _tween = new Tween();
    private List<(int cost, List<Entity> entities)> _spawnDefinitions;
    private Light2D _light = new Light2D();
    private int _currentWaveIndex = 0;
    private List<Entity> currentWaveEntitiesRemaining = 0;
    private int _nextSpawnRequireTotal = 0;
    private int _nextSpawnRequireCurrent = 0;
    private Random rng = new Random();

    private Gate(){}

    public Gate(List<(int, List<Entity>)> spawnDefinitions){
        _spawnDefinitions = spawnDefinitions;
        _sprite.Frames = ResourceLoader.Load("res://entities/gates/Gate.tres") as SpriteFrames;

        AddChild(_sprite);
        AddChild(_light);
        AddChild(_tween);
    }

    public override void _Ready(){
        _tween.InterpolateProperty(_light, "color", new Color(255, 255, 255, 0f), new Color(255, 255, 255, 1f), 5f);
        _tween.InterpolateCallback(this, 5f, "BeginWave", _currentWaveIndex);
        _sprite.Play("default");
    }

    public void SetupNewWave(int waveIndex = 0){
        currentWaveEntitiesRemaining = _spawnDefinitions[waveIndex].entities;
        var count = currentWaveEntitiesRemaining.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i) {
            var r = rng.Next(i + 1);
            var tmp = currentWaveEntitiesRemaining[i];
            currentWaveEntitiesRemaining[i] = currentWaveEntitiesRemaining[r];
            currentWaveEntitiesRemaining[r] = tmp;
        }
        foreach(Entity entity in _spawnDefinitions[_currentWaveIndex].entities){
            entity.Connect("Dead", this, "OnEntityDead");
        }
    }

    public void ContinueWave(){
        var waveEntities = _spawnDefinitions[_currentWaveIndex].entities;
        if (waveEntities.Count == 0){
            var numEnemies = rng.Next(1, waveEntities.Count);
        } else{
            SetupNewWave(++_currentWaveIndex);
        }
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
