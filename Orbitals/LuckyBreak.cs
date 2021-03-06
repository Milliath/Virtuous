﻿using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Virtuous.Tools;

namespace Virtuous.Orbitals
{
    public class LuckyBreak_Item : OrbitalItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lucky Break");
            Tooltip.SetDefault("The cards shuffle every few seconds, each giving individual effects\nHearts increase movement speed and life regeneration\nDiamonds make enemies drop more coins\nSpades increase all critical strike chance by " + LuckyBreak_Proj.CritBuff + "%\nClubs REDUCE all damage by " + LuckyBreak_Proj.DamageDebuff + "%\nAligns with either magic or melee users");
        }

        public override void SetOrbitalDefaults()
        {
            type = OrbitalID.LuckyBreak;
            duration = 42 * 60;
            amount = 5;

            item.width = 40;
            item.height = 34;
            item.damage = 35;
            item.knockBack = 2f;
            item.mana = 50;
            item.rare = 7;
            item.value = Item.sellPrice(0, 20, 0, 0);
            item.useStyle = 2;
            item.useTime = 15;
            item.useAnimation = item.useTime;
        }
    }


    public class LuckyBreak_Proj : OrbitalProjectile
    {
        public override int Type => OrbitalID.LuckyBreak;
        public override int DyingTime => 30;
        public override int FadeTime => 15;
        public override int OriginalAlpha => 0;
        public override float BaseDistance => _BaseDistance;
        public override float OrbitingSpeed => 0.0f * RevolutionPerSecond;
        public override float OscillationSpeedMax => 15f / 30;
        public override float OscillationAcc => OscillationSpeedMax / 20;
        public override float DyingSpeed => 20;

        public override bool isDoingSpecial => true; //Always keeps increasing specialFunctionTimer

        public const int _BaseDistance = 65;
        public const int CycleTime = 7 * 60; //Time between shuffles
        public const int ShuffleTime = 30; //Time of CycleTime in which the cards shuffle
        public const int ShuffleSpeed = (_BaseDistance - 2) * 2 / ShuffleTime; //Distance it takes to go back and forth over the time it will take to do it
        public const int Hearts = 0, Diamonds = 1, Spades = 2, Clubs = 3; //Frames

        public const int CritBuff = 7;
        public const int DamageDebuff = 7; //In percent


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Card");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetOrbitalDefaults()
        {
            projectile.width = 22;
            projectile.height = 28;
        }


        private void ShuffleCard()
        {
            projectile.frame = RandomInt(Main.projFrames[projectile.type]);
        }

        public override void PlayerEffects()
        {
            //Shuffle sound
            if (!isFirstTick && specialFunctionTimer == CycleTime - ShuffleTime / 2)
            {
                Main.PlaySound(18, player.Center);
            }
            
            //Individual card buffs
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].owner == projectile.owner && Main.projectile[i].type == projectile.type)
                {
                    switch (Main.projectile[i].frame)
                    {
                        case Hearts:
                            player.runAcceleration *= 1.2f;
                            player.maxRunSpeed *= 1.2f;
                            player.lifeRegen += 1;
                            break;

                        case Spades:
                            player.meleeCrit  += CritBuff;
                            player.magicCrit  += CritBuff;
                            player.rangedCrit += CritBuff;
                            player.thrownCrit += CritBuff;
                            break;

                        case Clubs:
                            player.meleeDamage  -= DamageDebuff / 100f;
                            player.magicDamage  -= DamageDebuff / 100f;
                            player.rangedDamage -= DamageDebuff / 100f;
                            player.thrownDamage -= DamageDebuff / 100f;
                            orbitalPlayer.damageBuffFromOrbitals -= DamageDebuff / 100f;
                            break;
                    }
                }
            }
        }

        public override void FirstTick()
        {
            RotatePosition(-FullCircle / 4); //Make the first card be above the player instead of to the right

            specialFunctionTimer = CycleTime - ShuffleTime / 2; //Puts the card in the middle of the shuffling motion
            SetDistance(2);
            oscillationSpeed = ShuffleSpeed;
        }

        public override bool PreMovement()
        {
            return !isDying;
        }

        public override void Movement()
        {
            if (specialFunctionTimer >= CycleTime - ShuffleTime) //Shuffling motion
            {
                if (specialFunctionTimer == CycleTime - ShuffleTime) //First tick
                {
                    SetDistance(BaseDistance);
                    oscillationSpeed = ShuffleSpeed;
                    direction = Inwards;
                }

                else if (specialFunctionTimer == CycleTime - ShuffleTime / 2) //Middlepoint of the motion
                {
                    ShuffleCard();
                    direction = Outwards;
                }

                else if (specialFunctionTimer == CycleTime - 1) //Last tick
                {
                    SetDistance(BaseDistance);
                    oscillationSpeed = OscillationSpeedMax;
                    specialFunctionTimer = 0;
                    return;
                }

                AddDistance(oscillationSpeed * (direction ? +1 : -1));
                RotatePosition(OrbitingSpeed);
            }
            else //Normal movement
            {
                base.Movement();
            }
        }

        public override void Dying()
        {
            projectile.rotation += 5 * RevolutionPerSecond;
            projectile.velocity.Y += 2f; //Gravity
            projectile.position += projectile.velocity; //Applies velocity as orbitals normally don't
        }


        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (isDying) damage *= 7;

            if (projectile.frame == Diamonds && target.lifeMax > 5 && !target.immortal)
            {
                target.AddBuff(BuffID.Midas, 7 * 60);
                int type = OneIn(10) ? (OneIn(10) ? ItemID.GoldCoin : ItemID.SilverCoin) : ItemID.CopperCoin;
                int amount = RandomInt(3, 15);
                int newItem = Item.NewItem(target.position, target.width, target.height, type, amount);
                if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendData(MessageID.SyncItem, -1, -1, null, newItem); //Syncs to multiplayer
            }
        }
        public override void ModifyHitPvp(Player target, ref int damage, ref bool crit)
        {
            if (isDying) damage *= 7;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255) * projectile.Opacity;
        }
    }
}