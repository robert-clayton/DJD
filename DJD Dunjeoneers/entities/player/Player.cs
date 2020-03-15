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
    private Light2D light = new Light2D();
    private Vector2 baseLightScale = new Vector2(.5f, .5f);
    private bool usePrimary = false;
    private bool useSecondary = false;
    private Tween lightTween = new Tween();

    public Player() : base(){}

    public Player(Vector2 pos) : base(){
        maxHealth = curHealth = 100f;
        SetCollisionMaskBit(1, true);
        sprite.Frames = ResourceLoader.Load("res://entities/player/Player.tres") as SpriteFrames;
        sprite.Play("default");
        ChangeState(state);
        Position = pos;

        var hurtArea = new Area2D();
        var hurtCollider = new CollisionShape2D();
        var hurtBoxShape = new RectangleShape2D();
        hurtArea.SetCollisionLayerBit(19, true);
        hurtArea.SetCollisionLayerBit(0, false);
        hurtArea.CollisionMask = 0;
        hurtArea.Name = "HurtBox";
        hurtBoxShape.Extents = new Vector2(2f, 4f);
        hurtCollider.Shape = hurtBoxShape;
        hurtArea.AddChild(hurtCollider);
        hurtArea.Connect("body_entered", this, "_OnTouchedByEnemy");
        AddChild(hurtArea);

        Texture gradient = ResourceLoader.Load("res://assets/gradients/radial.png") as Texture;
        light.Texture = gradient;
        light.Scale = baseLightScale;
        light.Color = new Color(1f, 1f, 1f, .3f);
        AddChild(light);

        AddChild(lightTween);

        AddToGroup("Players");
    }

    public override void _EnterTree(){
        GetTree().Root.CallDeferred("add_child", abilityManager);
    }

    public override void _Process(float delta){
        base._Process(delta);
        if (state == EPlayerState.STATE_DYING || state == EPlayerState.STATE_STUNNED || state == EPlayerState.STATE_DODGING) return;
        if (usePrimary)
            abilityManager.Invoke(primary, GlobalPosition.DirectionTo(GetGlobalMousePosition()), GlobalPosition, 2);
        if (useSecondary)
            abilityManager.Invoke(secondary, GlobalPosition.DirectionTo(GetGlobalMousePosition()), GlobalPosition, 2);
    }

    public override void _PhysicsProcess(float delta){
        base._PhysicsProcess(delta);
        if (state == EPlayerState.STATE_DYING || state == EPlayerState.STATE_STUNNED || state == EPlayerState.STATE_DODGING) return;
        Vector2 motion = new Vector2(
            Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left"),
            Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up")
        );
        if (motion.Length() == 0){
            SlowDown();
        } else velocity = (velocity + motion.Normalized() * acceleration * delta).Clamped(maxVelocity * delta);
        KinematicCollision2D result = MoveAndCollide(velocity);
        if (IsInstanceValid(result)) MoveAndCollide(result.Normal.Slide(motion));
    }

    public override void _UnhandledInput(InputEvent @event){
        usePrimary = Input.IsActionPressed("attack_primary");
        useSecondary = Input.IsActionPressed("attack_secondary");

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

    private void _OnTouchedByEnemy(Enemy enemy){
        Damage(maxHealth/4f, Position.DirectionTo(enemy.Position) * -100f);
    }

    public override void Damage(float damage, Vector2 knockback = default(Vector2)){
        if (curHealth == 0) return;
        base.Damage(damage, knockback);
        float healthPercent = curHealth / maxHealth;        
        Vector2 newScale = baseLightScale * (float)(healthPercent * healthPercent * (3f - 2f * healthPercent) * .7 + .3);
        lightTween.Stop(light, "scale");
        lightTween.InterpolateProperty(light, "scale", light.Scale, newScale, .3f);
        lightTween.Start();
    }

    public override void PrepareDeath(){
        base.PrepareDeath();
        SetCollisionLayerBit(19, false);
        GetNode<Area2D>("HurtBox").QueueFree();
        ChangeState(EPlayerState.STATE_DYING);
    }

    public override void Die(){
        base.Die();
    }

    public void ChangeState(EPlayerState newState){
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

