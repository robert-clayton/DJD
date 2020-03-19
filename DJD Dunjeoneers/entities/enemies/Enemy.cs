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

    public Vector2 moveAreaStart;
    public Vector2 moveAreaEnd;
    protected Vector2 _moveTargetNone = new Vector2(-100000, -10000);
    protected Vector2 _moveTarget = new Vector2(-100000, -10000);
    [Export] public EEnemyState _state = EEnemyState.STATE_IDLE;
    protected Player _player;
    protected Area2D _hurtArea = new Area2D();
    protected Area2D _alertArea = new Area2D();
    protected float _touchDamage = 25f;

    protected Enemy(int size = 8) : base(size){}

    public virtual void Initialize(Vector2 _position, Vector2 _moveAreaStart, Vector2 _moveAreaEnd)
    {
        SetCollisionMaskBit(1, true);
        CollisionLayer = 0;

        Position = _position;
        moveAreaStart = _moveAreaStart;
        moveAreaEnd = _moveAreaEnd;
        
        var hurtCollider = new CollisionShape2D();
        var hurtBoxShape = new RectangleShape2D();
        _hurtArea.SetCollisionLayerBit(0, false);
        _hurtArea.SetCollisionLayerBit(2, true);
        _hurtArea.SetCollisionMaskBit(19, true);
        _hurtArea.SetCollisionMaskBit(0, false);
        _hurtArea.Name = "HurtBox";
        hurtBoxShape.Extents = new Vector2(size/2, size/2);
        hurtCollider.Shape = hurtBoxShape;
        _hurtArea.AddChild(hurtCollider);
        AddChild(_hurtArea);

        
        var alertCollider = new CollisionShape2D();
        var alertBoxShape = new CircleShape2D();
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
        if (curHealth == 0) return;
        base.Damage(damage, knockback);
        GetParent().GetNode("Game").GetNode<Camera>("Camera").ShakeFromDamage(2f);

        if (GetTree().GetNodesInGroup("Players").Count > 0){
            _player = GetTree().GetNodesInGroup("Players")[0] as Player;
            _moveTarget = _player.Position;
            ChangeState(EEnemyState.STATE_ALERTED);
        }
    }

    public override void PrepareDeath(){
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

    public Vector2 RandomPointInMovableArea(bool force = false){
        return new Vector2(
            rng.Next((int)moveAreaStart.x, (int)moveAreaEnd.x), 
            rng.Next((int)moveAreaStart.y, (int)moveAreaEnd.y)
        );
    }

    public override void _Process(float delta){
        base._Process(delta);
        if (curHealth < 0)
            PrepareDeath();
        else if (_moveTarget == _moveTargetNone){
            _moveTarget = RandomPointInMovableArea();
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
            velocity = (velocity + Position.DirectionTo(_moveTarget) * acceleration * delta).Clamped(maxVelocity * delta);
            sprite.FlipH = velocity.x < 0;
            var result = MoveAndCollide(velocity);
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
