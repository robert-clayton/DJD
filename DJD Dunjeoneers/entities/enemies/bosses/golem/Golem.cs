using Godot;
using System;

public class Golem : Boss{
    protected Tween tween = new Tween();
    protected Timer lurchTimer = new Timer();
    protected Vector2 lurchTarget = default(Vector2);
    protected Vector2 lurchDirection = default(Vector2);

    private float overloadRange = 100;
    private float overloadDamage = 50;
    private Area2D overloadArea = new Area2D();
    Particles2D vent = new Particles2D();
    CollisionPolygon2D polygon = new CollisionPolygon2D();
    ParticlesMaterial mat = ResourceLoader.Load("res://entities/enemies/bosses/golem/overload_vent.tres") as ParticlesMaterial;
    AudioStreamPlayer findTargetSfx = new AudioStreamPlayer();

    public Golem(Vector2 _position, Vector2 _moveAreaStart, Vector2 _moveAreaEnd) : base(_position, _moveAreaStart, _moveAreaEnd){
        findTargetSfx.Stream = ResourceLoader.Load("res://assets/audio/boss_golem_find_target.wav") as AudioStreamSample;
        sprite.Frames = ResourceLoader.Load("res://entities/enemies/bosses/golem/Golem.tres") as SpriteFrames;
        ChangeState(EEnemyState.STATE_IDLE);
        MaxVelocity = 40f;

        // Overload material setup
        vent.ProcessMaterial = mat;
        vent.SpeedScale = 2;
        vent.Amount = 2500;
        vent.Emitting = false;
        mat.InitialVelocity = overloadRange;

        // Overload area setup
        Vector2[] points = {
            new Vector2(-10, -3),
            new Vector2(75, -80),
            new Vector2(110, -45),
            new Vector2(120, 0),
            new Vector2(110, 45),
            new Vector2(75, 80),
            new Vector2(-10, 3)
        };
        polygon.Polygon = points;
        polygon.Disabled = true;
        overloadArea.CollisionLayer = 0;
        overloadArea.SetCollisionMaskBit(0, false);
        overloadArea.SetCollisionMaskBit(19, true);
        overloadArea.Connect("area_entered", this, "_OnPlayerEnter_OverloadVent");

        lurchTimer.OneShot = true;
        lurchTimer.Connect("timeout", this, "FindLurchTarget");
        tween.Connect("tween_completed", this, "OverloadVent");

        overloadArea.AddChild(vent);
        overloadArea.AddChild(polygon);
        AddChild(overloadArea);
        AddChild(tween);
        AddChild(lurchTimer);
        AddChild(findTargetSfx);
    }

    public override void _Ready(){
        base._Ready();
        findTargetSfx.VolumeDb = globals.EnemySfxDb;
        findTargetSfx.StreamPaused = globals.EnemySfxDb == globals.minDb;
        StartNextLurch();
    }

    protected override void _OnEnemySfxDbChanged(float value){
        base._OnEnemySfxDbChanged(value);
        findTargetSfx.VolumeDb = value;
    }

    protected override void _OnEnemySfxPaused(bool paused){
        base._OnEnemySfxPaused(paused);
        findTargetSfx.StreamPaused = paused;
    }

    public override void _PhysicsProcess(float delta){
        ProcessKnockback(delta);
    }

    private void _OnPlayerEnter_OverloadVent(Area2D playerArea){
        Player player = playerArea.GetParent<Player>();
        player.Damage(overloadDamage);
    }

    public void OverloadVent(Godot.Object obj, NodePath key){
        if (key.ToString() == ":global_position"){
            vent.Emitting = true;
            polygon.Disabled = false;
            overloadArea.Rotation = lurchDirection.Angle();
            tween.InterpolateCallback(this, .5f, "Disable_OverloadVent");
            tween.InterpolateCallback(this, .5f, "StartNextLurch");
        }
    }

    public void Disable_OverloadVent(){
        vent.Emitting = false;
        polygon.Disabled = true;
    }

    public void StartNextLurch(){
        var randomTimeScale = 1f + (float)(rng.NextDouble() * .1 - .2);
        lurchTimer.WaitTime = randomTimeScale + .5f;
        findTargetSfx.PitchScale = randomTimeScale;
        lurchTimer.Start();
        findTargetSfx.Play();
    }

    public void FindLurchTarget(){
        var playerList = GetTree().GetNodesInGroup("Players");
        findTargetSfx.Stop();
        if (playerList.Count > 0){
            Player player = playerList[0] as Player;
            var lurchTime = 1f;
            if (Position.DistanceTo(player.GlobalPosition) > lurchTime * MaxVelocity){
                lurchDirection = Position.DirectionTo(player.GlobalPosition);
                lurchTarget = Position + lurchDirection * MaxVelocity;
            } else {
                lurchDirection = Position.DirectionTo(player.GlobalPosition);
                lurchTarget = player.GlobalPosition;
                lurchTime *= GlobalPosition.DistanceTo(lurchTarget) / MaxVelocity;
            }
            sprite.FlipH = lurchDirection.x < 0;
            tween.Remove(this, "global_position");
            tween.InterpolateProperty(this, "global_position", Position, lurchTarget, lurchTime, easeType: Tween.EaseType.Out);
            tween.Start();
        }
    }
}

