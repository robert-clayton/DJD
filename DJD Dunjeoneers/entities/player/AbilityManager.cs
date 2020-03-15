using Godot;
using System;

public enum EAbilities{
    FIREBALL,
    PYROBLAST,
    CHAIN_LIGHTNING
}

public class AbilityManager : Node{
    public Timer cooldown = new Timer();

    public AbilityManager(){
        AddChild(cooldown);
        cooldown.OneShot = true;
    }

    public void Invoke(EAbilities ability, Vector2 _direction, Vector2 _pos, int _targetLayer){
        AbilityBase newAbility = null;
        if (cooldown.IsStopped()){
            switch (ability){
                case EAbilities.FIREBALL: 
                    newAbility = new Fireball(_direction, _pos, _targetLayer);
                    break;
                case EAbilities.PYROBLAST:
                    newAbility = new Pyroblast(_direction, _pos, _targetLayer);
                    break;
                case EAbilities.CHAIN_LIGHTNING:
                    newAbility = new ChainLightning(_direction, _pos, _targetLayer);
                    break;
                default: 
                    newAbility = new Fireball(_direction, _pos, _targetLayer);
                    break;
            }
            cooldown.WaitTime = newAbility.cooldown;
            cooldown.Start();
            AddChild(newAbility);
        }
    }
}
