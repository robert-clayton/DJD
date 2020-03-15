using Godot;
using System;

public class Enemy : Entity{
    public enum EEnemyState{
        STATE_IDLE,
        STATE_MOVING,
        STATE_STUNNED,
        STATE_DYING,
        STATE_ALERTED,
    }

    public Vector2 moveAreaStart;
    public Vector2 moveAreaEnd;
    protected Vector2 moveTargetNone = new Vector2(-100000, -10000);
    protected Vector2 moveTarget = new Vector2(-100000, -10000);
    [Export] public EEnemyState state = EEnemyState.STATE_IDLE;
    protected Player player;
    protected Area2D hurtArea = new Area2D();
    protected float touchDamage = 25f;

    public Enemy(){}

    public Enemy(Vector2 _position, Vector2 _moveAreaStart, Vector2 _moveAreaEnd, int size = 8) : base(size){
        SetCollisionMaskBit(1, true);
        CollisionLayer = 0;

        Position = _position;
        moveAreaStart = _moveAreaStart;
        moveAreaEnd = _moveAreaEnd;
        
        var hurtCollider = new CollisionShape2D();
        var hurtBoxShape = new RectangleShape2D();
        hurtArea.SetCollisionLayerBit(0, false);
        hurtArea.SetCollisionLayerBit(2, true);
        hurtArea.SetCollisionMaskBit(19, true);
        hurtArea.SetCollisionMaskBit(0, false);
        hurtArea.Name = "HurtBox";
        hurtBoxShape.Extents = new Vector2(size/2, size/2);
        hurtCollider.Shape = hurtBoxShape;
        hurtArea.AddChild(hurtCollider);
        AddChild(hurtArea);

        var alertArea = new Area2D();
        var alertCollider = new CollisionShape2D();
        var alertBoxShape = new CircleShape2D();
        alertArea.Name = "AlertCircle";
        alertArea.SetCollisionMaskBit(19, true);
        alertArea.SetCollisionMaskBit(0, false);
        alertArea.CollisionLayer = 0;
        alertBoxShape.Radius = 35;
        alertCollider.Shape = alertBoxShape;
        alertArea.AddChild(alertCollider);
        AddChild(alertArea);

        alertArea.Connect("area_entered", this, "_OnPlayerEnter");
        hurtArea.Connect("area_entered", this, "_OnPlayerTouch");
        AddToGroup("Enemies");
    }

    public override void _Ready(){
        base._Ready();
        globals.Connect("EnemySfxDbChanged", this, "_OnEnemySfxDbChanged");
        globals.Connect("EnemySfxPaused", this, "_OnEnemySfxPaused");
    }

    protected virtual void _OnEnemySfxDbChanged(float value){
        
    }

    protected virtual void _OnEnemySfxPaused(bool paused){
        
    }

    public override void Damage(float damage, Vector2 knockback = default(Vector2)){
        if (curHealth == 0) return;
        base.Damage(damage, knockback);
        GetParent().GetNode("Game").GetNode<Camera>("Camera").ShakeFromDamage(2f);

        if (GetTree().GetNodesInGroup("Players").Count > 0){
            player = GetTree().GetNodesInGroup("Players")[0] as Player;
            moveTarget = player.Position;
            ChangeState(EEnemyState.STATE_ALERTED);
        }
    }

    public override void PrepareDeath(){
        base.PrepareDeath();
        ChangeState(EEnemyState.STATE_DYING);
        RemoveFromGroup("Enemies");
        GetNode<Area2D>("HurtBox").QueueFree();
        GetNode<Area2D>("AlertCircle").QueueFree();
    }

    public void _OnPlayerEnter(Area2D playerArea){
        player = playerArea.GetParent<Player>();
        moveTarget = player.Position;
        ChangeState(EEnemyState.STATE_ALERTED);
    }

    public void _OnPlayerTouch(Area2D area){
        if (area.GetParent() is Entity){
            Entity sourceTouch = area.GetParent() as Entity;
            sourceTouch.Damage(touchDamage, hurtArea.GlobalPosition.DirectionTo(area.GlobalPosition)  * 100f);
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
        else if (moveTarget == moveTargetNone){
            moveTarget = RandomPointInMovableArea();
            ChangeState(EEnemyState.STATE_MOVING);
        } else if (state == EEnemyState.STATE_ALERTED){
            if (IsInstanceValid(player))
                moveTarget = player.Position;
            else
                player = null;
        }
    }

    public override void _PhysicsProcess(float delta){
        base._PhysicsProcess(delta);
        if (state == EEnemyState.STATE_IDLE || state == EEnemyState.STATE_STUNNED || state == EEnemyState.STATE_DYING) return;
        if (moveTarget != moveTargetNone){
            velocity = (velocity + Position.DirectionTo(moveTarget) * acceleration * delta).Clamped(maxVelocity * delta);
            sprite.FlipH = velocity.x < 0;
            var result = MoveAndCollide(velocity);
            if (IsInstanceValid(result)){
                MoveAndCollide(result.Normal.Slide(velocity));
                moveTarget = moveTargetNone;
                ChangeState(EEnemyState.STATE_IDLE);
            }
            if (Position.DistanceTo(moveTarget) < 0.2f){
                moveTarget = moveTargetNone;
                ChangeState(EEnemyState.STATE_IDLE);
            }
        }
    }

    public void ChangeState(EEnemyState newState){
        if (state == EEnemyState.STATE_DYING) return;
        switch (newState){
            case EEnemyState.STATE_IDLE:
                sprite.Play("default");
                sprite.SpeedScale = rng.Next(75, 100) / 100f * 0.5f;
                break;
            case EEnemyState.STATE_MOVING:
                sprite.Play("default");
                if (state == EEnemyState.STATE_ALERTED){
                    sprite.SpeedScale = rng.Next(75, 100) / 100f * 0.5f;
                }
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
        state = newState;
    }
}
