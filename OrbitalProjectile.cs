﻿using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Virtuous.Orbitals;
using static Virtuous.Tools;

namespace Virtuous
{
    public abstract class OrbitalProjectile : ModProjectile
    {
        /*
         * This class contains all the properties and methods used by orbitals.
         * The orbitals themselves will override these virtual members to obtain the exact behavior of each unique orbital.
         * The naming convention is: "PascalCasing" if the property is constant for all orbitals of a type, "camelCasing" if the property is variable across time and across different instances
         * Be sure to read the comments to better picture what each thing does
         */


        //Owner shortcuts
        public Player player => Main.player[projectile.owner];
        public OrbitalPlayer orbitalPlayer => player.GetModPlayer<OrbitalPlayer>();

        //Behavior
        public Vector2 relativePosition { get{ return projectile.velocity; } set{ projectile.velocity = value; } } //Relative position to the player, stored as velocity
        public float relativeDistance { get{ return relativePosition.Length(); } set{ relativePosition = relativePosition.OfLength(value); } } //Distance away from the player. Affects RelativePosition directly.
        public float oscillationSpeed { get{ return projectile.ai[0]; } set{ projectile.ai[0] = value; } } //Current speed of back-and-forth oscillation, Stored as ai[0]
        public bool direction { get { return projectile.ai[1] == 0; } set { projectile.ai[1] = value ? 0 : 1; } } //Direction of movement, inwards or outwards, used by default for oscillation. Stored as ai[1]
        public int specialFunctionTimer { get{ return (int)projectile.localAI[0]; } set{ projectile.localAI[0] = value; } } //Time passed since the special effect was used. Stored as localAI[0]

        //Characteristics
        public virtual int Type => OrbitalID.None; //The orbital ID associated with the projectile. Failing to provide one will cause an exception.
        public virtual int FadeTime => 0; //How many ticks the projectile fades away for, if any
        public virtual int DyingTime => 0; //How many ticks, if any, the projectile spends in "dying mode" at the end of its lifespan, during which no orbital items can be used
        public virtual int OriginalAlpha => 50; //Original alpha value of the projectile
        public virtual float BaseDistance => 50; //Distance it starts at
        public virtual float RotationSpeed => 0; //Speed at which the projectile's sprite rotates
        public virtual float OrbitingSpeed => 0; //Speed at which the projectile orbits around the player
        public virtual float DyingSpeed => 0; //Speed at which, by default, the projectile will shoot out in DyingTime
        public virtual float OscillationSpeedMax => 0;  //Speed limit. Translates into how far it can go before changing direction of movement
        public virtual float OscillationAcc => OscillationSpeedMax / 60; //Acceleration rate. Translates into how fast it reaches the point of direction change

        //Checks
        public virtual bool isFirstTick => (relativeDistance == 1.0f); //Whether it's the first tick of the orbital's life. Orbitals are always created with a velocity vector of size 1, but it's changed in the first tick
        public virtual bool isDying => (Main.myPlayer == projectile.owner && DyingTime > 0 && orbitalPlayer.time <= DyingTime); //Whether to treat this projectile as in dying mode
        public virtual bool isDoingSpecial => (Main.myPlayer == projectile.owner && orbitalPlayer.specialFunctionActive && !isDying); //Whether to run the special effect method or not


        //Utility methods
        public void SetPosition(Vector2? newPos = null) //Moves the orbital relative to the player
        {
            if (newPos != null) relativePosition = (Vector2)newPos;
            projectile.Center = player.MountedCenter + relativePosition;
        }
        public void RotatePosition(float radians) //Rotates the orbital relative to the player
        {
            SetPosition(relativePosition.RotatedBy(radians));
        }
        public void SetDistance(float newDistance) //Applies a new distance to the player and moves the orbital relative to the player
        {
            SetPosition(relativePosition.OfLength(newDistance));
        }
        public void AddDistance(float distance)
        {
            SetDistance(relativeDistance + distance);
        }

        public static OrbitalProjectile FindFirstOrbital(Mod mod, Player player, int id = OrbitalID.None) //Returns the first orbital found for the given player and type
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].owner == player.whoAmI)
                {
                    if (id == OrbitalID.None && Main.projectile[i].modProjectile as OrbitalProjectile != null || id > 0 && Main.projectile[i].type == mod.OrbitalProjectileType(id))
                    {
                        return Main.projectile[i].modProjectile as OrbitalProjectile;
                    }
                }
            }

            return null; //No orbital was found
        }



        public virtual void SetOrbitalDefaults()
        {
        }

        public sealed override void SetDefaults() //Safe way to set constant defaults
        {
            projectile.netImportant = true; //So it syncs more frequently in multiplayer
            projectile.penetrate = -1;
            projectile.friendly = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.usesIDStaticNPCImmunity = true; //Doesn't interfere with other piercing damage
            projectile.idStaticNPCHitCooldown = 10;
            projectile.alpha = OriginalAlpha;
            projectile.timeLeft = (DyingTime > 0) ? DyingTime : 2; //Time left gets reset every tick during the orbital's life

            SetOrbitalDefaults();
        }

        //Effects the orbital type will apply on the player while it is active. Only runs for the first orbital of a type. Called by OrbitalPlayer
        public virtual void PlayerEffects()
        {
        }

        //Only runs once at the beginning of the orbital's life
        public virtual void FirstTick()
        {
            SetDistance(BaseDistance);
            oscillationSpeed = OscillationSpeedMax;
            projectile.rotation = relativePosition.ToRotation();
        }

        //Returns whether to execute movement
        public virtual bool PreMovement()
        {
            return (!isDying && !isDoingSpecial); //By default doesn't do normal movement if it's dying or in special mode
        }

        //Main orbital behavior
        public virtual void Movement()
        {
            if (OscillationSpeedMax != 0) //Oscillation
            {
                if      (oscillationSpeed >= +OscillationSpeedMax) direction = Inwards;  //If it has reached the outwards speed limit, begin to switch direction
                else if (oscillationSpeed <= -OscillationSpeedMax) direction = Outwards; //If it has reached the inwards speed limit, begin to switch direction
                oscillationSpeed += OscillationAcc * (direction ? +1 : -1); //Accelerate in the corresponding direction
                AddDistance(oscillationSpeed);
            }

            RotatePosition(OrbitingSpeed); //Rotates the projectile around the player
            projectile.rotation += RotationSpeed; //Rotates the projectile itself
        }

        public virtual void PostMovement()
        {
        }


        //Executes special effect
        public virtual void SpecialFunction()
        {
        }


        //Only runs once at the beginning of DyingTime
        public virtual void DyingFirstTick()
        {
            projectile.velocity = relativePosition.OfLength(DyingSpeed); //Starts shooting-out motion
        }

        //Runs every tick during DyingTime
        public virtual void Dying()
        {
            projectile.velocity -= projectile.velocity.OfLength(DyingSpeed / DyingTime); //Slows down to a halt
            projectile.position += projectile.velocity; //Re-applies velocity as it would normally be nullified for orbitals
        }


        //Runs every tick after everything else. Used for fading away, light, etc.
        public virtual void PostAll()
        {
            if (FadeTime > 0 && Main.myPlayer == projectile.owner)
            {
                if (orbitalPlayer.time <= FadeTime)
                {
                    projectile.alpha += Math.Max(1, (int)((255f - OriginalAlpha) / FadeTime)); //Fades away completely over fadeTime
                }
                else
                {
                    projectile.alpha = OriginalAlpha; //Resets the alpha in case the time resets during fading time
                }
            }
        }


        //Head of the operation
        public sealed override void AI()
        {
            if (!orbitalPlayer.active[Type] && Main.myPlayer == projectile.owner) //Keep it alive only while the summon is active
            {
                projectile.netUpdate = true; //Sync to multiplayer
                projectile.Kill();
            }
            else
            {
                if (isFirstTick)
                {
                    projectile.netUpdate = true;
                    FirstTick();
                }

                if (PreMovement())
                {
                    Movement();
                    PostMovement();
                }

                if (isDoingSpecial)
                {
                    SpecialFunction();
                    specialFunctionTimer++;
                }
                else
                {
                    specialFunctionTimer = 0;
                }

                if (isDying) //timeLeft ticks down during dying time
                {
                    if (orbitalPlayer.time == DyingTime)
                    {
                        projectile.netUpdate = true;
                        DyingFirstTick();
                    }

                    Dying();
                }
                else //Keeps the orbital from dying naturally
                {
                    projectile.timeLeft = Math.Max(2, DyingTime);
                }


                PostAll();

                projectile.position -= projectile.velocity; //Reverses the effect of velocity so the orbital doesn't move by default
            }
        }


        //Syncs local ai slots in multiplayer
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(projectile.localAI[0]);
            writer.Write(projectile.localAI[1]);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            projectile.localAI[0] = reader.ReadSingle();
            projectile.localAI[1] = reader.ReadSingle();
        }

        public override bool? CanCutTiles()
        {
            return false; //So they don't become a lawnmower
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 100) * projectile.Opacity; //Fullbright
        }
    }
}