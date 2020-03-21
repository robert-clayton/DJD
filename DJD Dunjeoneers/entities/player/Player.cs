using Godot;
using System;

public class Player : Entity{
    public enum EPlayerState{
        STATE_IDLE,
        STATE_MOVING,
        STATE_STUNNED,
        STATE_DYING,
        STATE_DODGING,
    }

    private AbilityManager abilityManager = new AbilityManager();
    [Export] public EPlayerState state = EPlayerState.STATE_IDLE;
    [Export] public EAbilities primary = EAbilities.FIREBALL;
    [Export] public EAbilities secondary = EAbilities.CHAIN_LIGHTNING;
    private Light2D _light = new Light2D();
    private Vector2 _baseLightScale = new Vector2(.5f, .5f);
    private bool _usePrimary = false;
    private bool _useSecondary = false;
    private Tween _lightTween = new Tween();

    public Player() : base(){}

    public void Initialize(Vector2 position){
        base.Initialize(position);
        abilityManager.Player = this;
        SetCollisionMaskBit(1, true);
        sprite.Frames = ResourceLoader.Load("res://entities/player/Player.tres") as SpriteFrames;
        sprite.Play("default");
        ChangeState(state);

        Area2D hurtArea = new Area2D();
        CollisionShape2D hurtCollider = new CollisionShape2D();
        RectangleShape2D hurtBoxShape = new RectangleShape2D();
        hurtArea.SetCollisionLayerBit(19, true);
        hurtArea.SetCollisionLayerBit(0, false);
        hurtArea.CollisionMask = 0;
        hurtArea.Name = "HurtBox";
        hurtBoxShape.Extents = new Vector2(2f, 4f);
        hurtCollider.Shape = hurtBoxShape;
        
        hurtArea.Connect("body_entered", this, "OnTouchedByEnemy");
        

        Texture gradient = ResourceLoader.Load("res://assets/gradients/radial.png") as Texture;
        _light.Texture = gradient;
        _light.Scale = _baseLightScale;
        _light.Color = new Color(1f, 1f, 1f, .3f);
        
        hurtArea.AddChild(hurtCollider);
        AddChild(hurtArea);
        AddChild(_light);
        AddChild(_lightTween);
        AddToGroup("Players");
    }

    public override void _EnterTree(){
        GetTree().Root.CallDeferred("add_child", abilityManager);
    }

    public override void _Process(float delta){
        base._Process(delta);
        if (state == EPlayerState.STATE_DYING || state == EPlayerState.STATE_STUNNED || state == EPlayerState.STATE_DODGING) return;
        if (_usePrimary)
            abilityManager.Invoke(primary, GlobalPosition.DirectionTo(GetGlobalMousePosition()), GlobalPosition, 2);
        if (_useSecondary)
            abilityManager.Invoke(secondary, GlobalPosition.DirectionTo(GetGlobalMousePosition()), GlobalPosition, 2);
    }

    public override void _PhysicsProcess(float delta){
        base._PhysicsProcess(delta);
        if (state == EPlayerState.STATE_DYING || state == EPlayerState.STATE_STUNNED || state == EPlayerState.STATE_DODGING) return;

        Vector2 motion = new Vector2(
            Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left"),
            Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up")
        );
        SlowDown(motion, delta);
        if (motion.Length() > 0)
            velocity = (velocity + motion.Normalized() * Acceleration * delta).Clamped(MaxVelocity);
        KinematicCollision2D result = MoveAndCollide(velocity * delta);
        if (IsInstanceValid(result)){
            velocity = new Vector2(
                Mathf.Abs(result.Normal.x) > 0? 0 : velocity.x,
                Mathf.Abs(result.Normal.y) > 0? 0 : velocity.y
            );
            MoveAndCollide(result.Remainder);
        }
    }

    protected virtual void SlowDown(Vector2 motion, float delta){
        velocity = velocity.LinearInterpolate(new Vector2(
            (motion.x == 0? true : Mathf.Sign(motion.x) != Mathf.Sign(velocity.x))? 0 : velocity.x,
            (motion.y == 0? true : Mathf.Sign(motion.y) != Mathf.Sign(velocity.y))? 0 : velocity.y
            ), 5f * delta);
        if (velocity.Length() < 1f && motion.Length() == 0)
            velocity = new Vector2(0,0);
    }

    public override void _UnhandledInput(InputEvent @event){
        _usePrimary = Input.IsActionPressed("attack_primary");
        _useSecondary = Input.IsActionPressed("attack_secondary");

        if (Input.IsActionJustPressed("move_right") && !Input.IsActionPressed("move_left"))
            sprite.FlipH = false;
        if (Input.IsActionJustPressed("move_left") && !Input.IsActionPressed("move_right"))
            sprite.FlipH = true;

        if (Input.IsActionJustReleased("move_left") && Input.IsActionPressed("move_right"))
            sprite.FlipH = false;
        if (Input.IsActionJustReleased("move_right") && Input.IsActionPressed("move_left"))
            sprite.FlipH = true;

        if (Input.IsActionPressed("move_left") || Input.IsActionPressed("move_right")
            || Input.IsActionPressed("move_up") || Input.IsActionPressed("move_down")){
                ChangeState(EPlayerState.STATE_MOVING);
            } else ChangeState(EPlayerState.STATE_IDLE);
    }

    private void OnTouchedByEnemy(Enemy enemy){
        Damage(MaxHealth/4f, Position.DirectionTo(enemy.Position) * -100f);
    }

    public override void Damage(float damage, Vector2 knockback = default(Vector2)){
        if (CurHealth == 0) return;
        base.Damage(damage, knockback);
        float healthPercent = CurHealth / MaxHealth;        
        Vector2 newScale = _baseLightScale * (float)(healthPercent * healthPercent * (3f - 2f * healthPercent) * .7 + .3);
        _lightTween.Stop(_light, "scale");
        _lightTween.InterpolateProperty(_light, "scale", _light.Scale, newScale, .3f);
        _lightTween.Start();
    }

    protected override void PrepareDeath(){
        base.PrepareDeath();
        SetCollisionLayerBit(19, false);
        GetNode<Area2D>("HurtBox").QueueFree();
        ChangeState(EPlayerState.STATE_DYING);
    }

    protected override void Die(){
        base.Die();
    }

    protected void ChangeState(EPlayerState newState){
        if (state == EPlayerState.STATE_DYING) return;
        switch (newState){
            case EPlayerState.STATE_IDLE:
                sprite.Play("default");
                break;
            case EPlayerState.STATE_MOVING:
                if (state != EPlayerState.STATE_MOVING) sprite.Play("default");
                break;
            case EPlayerState.STATE_STUNNED:
                sprite.Play("default");
                break;
            case EPlayerState.STATE_DYING:
                break;
            default:
                sprite.Play("default");
                break;
        }
        state = newState;
    }
}

