using Godot;
using System;
using System.Collections.Generic;

public class Gate : Node2D{

    [Signal] public delegate void Complete();
    [Signal] public delegate void ThresholdReached();


    public float RedChance = .1f;
    public bool ActivateOnReady {get; set;} = false;
    public int TotalValue {
        get{
            var total = 0;
            foreach (Entity entity in EntitiesToSpawn) total += entity.DeathValue;
            foreach (Entity entity in EntitiesActive) total += entity.DeathValue;
            foreach (Entity entity in EntitiesKilled) total += entity.DeathValue;
            return total;
        }}
    public int StoredValue {get{
            var total = 0;
            foreach (Entity entity in EntitiesToSpawn) total += entity.DeathValue;
            foreach (Entity entity in EntitiesActive) total += entity.DeathValue;
            return total;
        }}
    public int CurrentValue {get{
        var total = 0;
        foreach (Entity entity in EntitiesKilled) total += entity.DeathValue;
        return total;
    }}
    public bool IsComplete {get{
        return CurrentValue == TotalValue;
    }}
    
    private int _threshold = 0;
    
    public List<Entity> EntitiesToSpawn {get; set;} = new List<Entity>();
    public List<Entity> EntitiesActive {get; protected set;} = new List<Entity>();
    public List<Entity> EntitiesKilled {get; protected set;} = new List<Entity>();

    private AnimatedSprite _sprite = new AnimatedSprite();
    private Tween _tween = new Tween();
    private Timer _spawnTimer = new Timer();
    private Light2D _light = new Light2D();
    private Texture _gradient = ResourceLoader.Load("res://assets/gradients/radial.png") as Texture;
    private Random _rng = new Random();

    public override void _Ready(){
        if (ActivateOnReady) Activate();
    }

    public void Init(){
        AddChild(_sprite);
        AddChild(_light);
        AddChild(_tween);
        AddChild(_spawnTimer);

        _sprite.Frames = ResourceLoader.Load("res://entities/gates/Gate.tres") as SpriteFrames;
        _light.Texture = _gradient;
        _light.Scale = new Vector2(.1f, .1f);
        _light.Color = new Color(1f, 1f, 1f, 0f);
        _tween.InterpolateProperty(_light, "color", _light.Color, new Color(_light.Color.r, _light.Color.g, _light.Color.b, .4f), 2f);
        _tween.InterpolateCallback(_spawnTimer, 2f, "start");
        _spawnTimer.WaitTime = .2f;
        _spawnTimer.Connect("timeout", this, "CheckToSpawn");

        _threshold = TotalValue * 2 / 3;
    }

    public void Activate(){
        _sprite.Play("default");
        _tween.Start();
    }

    private void CheckToSpawn(){
        if (EntitiesToSpawn.Count > 0){
            var entity = EntitiesToSpawn[0];
            GetTree().Root.AddChild(entity);
            entity.Position = GlobalPosition;
            entity.Connect("Dead", this, "OnEntityDead");
            EntitiesActive.Add(entity);
            EntitiesToSpawn.RemoveAt(0);
        }
    }

    private void GoRed(){

        RedChance = 0f;
        _sprite.Modulate = new Color(1f, .4f, .4f, 1f);
        _tween.InterpolateProperty(_light, "scale", _light.Scale, _light.Scale * 5, 1f);
        _tween.InterpolateProperty(_light, "color", _light.Color, new Color(_light.Color.r, .4f, .4f, .5f), 1f);

        _tween.InterpolateProperty(_sprite, "scale", Scale, Scale * 1.5f, 1f);

        foreach (Entity entity in EntitiesToSpawn){
            _tween.InterpolateProperty(entity, "scale", entity.Scale, entity.Scale * 2, 1f);
            entity.MaxHealth = entity.CurHealth = entity.MaxHealth * 2;
            entity.MaxVelocity *= 2;
        }
        foreach (Entity entity in EntitiesActive){
            _tween.InterpolateProperty(entity, "scale", entity.Scale, entity.Scale * 2, 1f);
            entity.MaxHealth = entity.CurHealth = entity.MaxHealth * 2;
            entity.MaxVelocity *= 2;
        }

        _tween.Start();
    }

    private void Finish(){
        RedChance = 0f;
        _tween.InterpolateProperty(_light, "color", _light.Color, new Color(_light.Color.r, _light.Color.g, _light.Color.b, .0f), 2f);
        _tween.Start();
        EmitSignal(nameof(Complete));
    }

    private void OnEntityDead(Entity deadEntity){
        EntitiesKilled.Add(deadEntity);
        EntitiesActive.Remove(deadEntity);
        
        if (Position.DistanceTo(deadEntity.GlobalPosition) < 15f){
            if (_rng.Next(0, 100) / 100f < RedChance)
                GoRed();
        }

        if (StoredValue >= _threshold) EmitSignal(nameof(ThresholdReached));
        if (EntitiesActive.Count == 0 && EntitiesToSpawn.Count == 0) Finish();
    }
}
