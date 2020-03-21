using Godot;
using System;

public abstract class Enemy : Entity{
    public enum EEnemyState{
        STATE_IDLE,
        STATE_MOVING,
        STATE_STUNNED,
        STATE_DYING,
        STATE_ALERTED,
    }
    protected Vector2 _moveTargetNone = new Vector2(-100000, -10000);
    protected Vector2 _moveTarget = new Vector2(-100000, -10000);
    [Export] public EEnemyState _state = EEnemyState.STATE_IDLE;
    protected Player _player;
    protected Area2D _hurtArea = new Area2D();
    protected Area2D _alertArea = new Area2D();
    protected float _touchDamage = 25f;

    protected Enemy() : base(){}

    public override void Initialize(Vector2 position, int size = 8){
        base.Initialize(Position, size: size);
        SetCollisionMaskBit(1, true);
        CollisionLayer = 0;
        CollisionShape2D hurtCollider = new CollisionShape2D();
        RectangleShape2D hurtBoxShape = new RectangleShape2D();
        _hurtArea.SetCollisionLayerBit(0, false);
        _hurtArea.SetCollisionLayerBit(2, true);
        _hurtArea.SetCollisionMaskBit(19, true);
        _hurtArea.SetCollisionMaskBit(0, false);
        _hurtArea.Name = "HurtBox";
        hurtBoxShape.Extents = new Vector2(size/2f, size/2f);
        hurtCollider.Shape = hurtBoxShape;
        _hurtArea.AddChild(hurtCollider);
        AddChild(_hurtArea);

        
        CollisionShape2D alertCollider = new CollisionShape2D();
        CircleShape2D alertBoxShape = new CircleShape2D();
        _alertArea.Name = "AlertCircle";
        _alertArea.SetCollisionMaskBit(19, true);
        _alertArea.SetCollisionMaskBit(0, false);
        _alertArea.CollisionLayer = 0;
        alertBoxShape.Radius = 35;
        alertCollider.Shape = alertBoxShape;
        _alertArea.AddChild(alertCollider);
        AddChild(_alertArea);

        _alertArea.Connect("area_entered", this, "_OnPlayerEnter");
        _hurtArea.Connect("area_entered", this, "_OnPlayerTouch");
        AddToGroup("Enemies");
    }

    public override void _Ready(){
        base._Ready();
        globals.Connect("EnemySfxDbChanged", this, "_OnEnemySfxDbChanged");
        globals.Connect("EnemySfxPaused", this, "_OnEnemySfxPaused");
    }

    protected virtual void _OnEnemySfxDbChanged(float value){}

    protected virtual void _OnEnemySfxPaused(bool paused){}

    public override void Damage(float damage, Vector2 knockback = default(Vector2)){
        if (CurHealth == 0) return;
        base.Damage(damage, knockback);
        GetParent().GetNode("Game").GetNode<Camera>("Camera").ShakeFromDamage(2f);

        if (GetTree().GetNodesInGroup("Players").Count > 0){
            _player = GetTree().GetNodesInGroup("Players")[0] as Player;
            _moveTarget = _player.Position;
            ChangeState(EEnemyState.STATE_ALERTED);
        }
    }

    protected override void PrepareDeath(){
        base.PrepareDeath();
        ChangeState(EEnemyState.STATE_DYING);
        RemoveFromGroup("Enemies");
        _hurtArea.QueueFree();
        _alertArea.QueueFree();
    }

    public void _OnPlayerEnter(Area2D playerArea){
        _player = playerArea.GetParent<Player>();
        _moveTarget = _player.Position;
        ChangeState(EEnemyState.STATE_ALERTED);
    }

    public void _OnPlayerTouch(Area2D area){
        if (area.GetParent() is Entity){
            Entity sourceTouch = area.GetParent() as Entity;
            sourceTouch.Damage(_touchDamage, _hurtArea.GlobalPosition.DirectionTo(area.GlobalPosition)  * 100f);
        }
    }

    public override void _Process(float delta){
        base._Process(delta);
        if (CurHealth < 0)
            PrepareDeath();
        else if (_moveTarget == _moveTargetNone){
            _moveTarget = Position + RandomDirection() * MaxVelocity * 5f;
            ChangeState(EEnemyState.STATE_MOVING);
        } else if (_state == EEnemyState.STATE_ALERTED){
            if (IsInstanceValid(_player)) _moveTarget = _player.Position;
            else _player = null;
        }
    }

    public override void _PhysicsProcess(float delta){
        base._PhysicsProcess(delta);
        if (_state == EEnemyState.STATE_IDLE || _state == EEnemyState.STATE_STUNNED || _state == EEnemyState.STATE_DYING) return;
        if (_moveTarget != _moveTargetNone){
            velocity = (velocity + Position.DirectionTo(_moveTarget) * Acceleration * delta).Clamped(MaxVelocity * delta);
            sprite.FlipH = velocity.x < 0;
            KinematicCollision2D result = MoveAndCollide(velocity);
            if (IsInstanceValid(result)){
                MoveAndCollide(result.Normal.Slide(velocity));
                _moveTarget = _moveTargetNone;
                ChangeState(EEnemyState.STATE_IDLE);
            }
            if (Position.DistanceTo(_moveTarget) < 0.2f){
                _moveTarget = _moveTargetNone;
                ChangeState(EEnemyState.STATE_IDLE);
            }
        }
    }

    public void ChangeState(EEnemyState newState){
        if (_state == EEnemyState.STATE_DYING) return;
        switch (newState){
            case EEnemyState.STATE_IDLE:
                sprite.Play("default");
                sprite.SpeedScale = rng.Next(75, 100) / 100f * 0.5f;
                break;
            case EEnemyState.STATE_MOVING:
                sprite.Play("default");
                if (_state == EEnemyState.STATE_ALERTED)
                    sprite.SpeedScale = rng.Next(75, 100) / 100f * 0.5f;
                break;
            case EEnemyState.STATE_STUNNED:
                sprite.Play("default");
                break;
            case EEnemyState.STATE_ALERTED:
                sprite.Play("default");
                sprite.SpeedScale = 2;
                break;
            case EEnemyState.STATE_DYING:
                sprite.Stop();
                break;
            default:
                sprite.Play("default");
                break;
        }
        _state = newState;
    }
}
