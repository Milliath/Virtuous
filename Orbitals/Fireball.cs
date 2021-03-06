﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Virtuous.Tools;

namespace Virtuous.Orbitals
{
    class Fireball_Item : OrbitalItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fireball");
            Tooltip.SetDefault("Burn nearby enemies\nRight-Click after summoning for a fire burst\nAligns with either magic or melee users");
        }

        public override void SetOrbitalDefaults()
        {
            type = OrbitalID.Fireball;
            duration = 20 * 60;
            amount = 1;
            specialFunctionType = SpecialRightClick;

            item.width = 30;
            item.height = 30;
            item.damage = 50;
            item.knockBack = 3f;
            item.mana = 20;
            item.value = Item.sellPrice(0, 8, 0, 0);
            item.rare = 5;
            item.useTime = 50;
            item.useAnimation = item.useTime;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.HellstoneBar, 10);
            recipe.AddIngredient(ItemID.TatteredCloth, 1);
            recipe.AddTile(TileID.Bookcases);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }


    class Fireball_Proj : OrbitalProjectile
    {
        public override int Type => OrbitalID.Fireball;
        public override int DyingTime => 60;
        public override int FadeTime => DyingTime;
        public override float BaseDistance => 90;
        public override float OrbitingSpeed => 0.5f * RevolutionPerSecond;
        public override float RotationSpeed => OrbitingSpeed;

        private const int OriginalSize = 30;
        private const int BurstSize = OriginalSize * 4;


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fireball");
        }

        public override void SetOrbitalDefaults()
        {
            projectile.width = OriginalSize;
            projectile.height = OriginalSize;
        }

        public override void FirstTick()
        {
            base.FirstTick();

            for (int i = 0; i < 15; i++)
            {
                Dust newDust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.SolarFlare);
                newDust.velocity *= 2;
            }
        }

        public override bool PreMovement()
        {
            return true; //Always move even during special effect or death
        }

        public override void PostMovement()
        {
            for (int i = 0; i < 7; i++) //Dust
            {
                Dust newDust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, Utils.SelectRandom<int>(Main.rand, new int[] { DustID.Fire, DustID.SolarFlare, 158 }), 0f, 0f, projectile.alpha, default(Color), 1f);
                if (newDust.type == DustID.SolarFlare) newDust.scale = 1.5f;
                else newDust.velocity *= 2;
                newDust.noGravity = true;
            }
        }

        public override void SpecialFunction()
        {
            if (specialFunctionTimer == 0) //First tick
            {
                ResizeProjectile(projectile.whoAmI, BurstSize, BurstSize, true);
                Main.PlaySound(SoundID.Item14, projectile.Center);
                for (int i = 0; i < 6; i++)
                {
                    PostMovement(); //Dust
                    Dust newDust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.SolarFlare, 0f, 0f, projectile.alpha, default(Color), 2.5f);
                    newDust.noGravity = true;
                }

                projectile.Damage(); //Damages enemies instantly
            }

            else if (specialFunctionTimer > 4) //Last tick
            {
                ResizeProjectile(projectile.whoAmI, OriginalSize, OriginalSize, true);
                orbitalPlayer.specialFunctionActive = false;
            }
        }

        public override void DyingFirstTick() //Nothing
        {
        }

        public override void Dying()
        {
            //Gains more orbiting speed
            float extraRotationFactor = 7f * (DyingTime - projectile.timeLeft) / (float)DyingTime;
            RotatePosition(OrbitingSpeed * extraRotationFactor);
            projectile.rotation += RotationSpeed * extraRotationFactor;
        }


        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (isDying || isDoingSpecial) damage *= 2;
            target.AddBuff(BuffID.OnFire, 5 * 60);
        }
        public override void ModifyHitPvp(Player target, ref int damage, ref bool crit)
        {
            if (isDying || isDoingSpecial) damage *= 2;
            target.AddBuff(BuffID.OnFire, 5 * 60);
        }
    }
}
