using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Virtuous.Projectiles;

namespace Virtuous.Items
{
    public class CrusherMace : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crusher Mace");
            Tooltip.SetDefault("\"To shreds, you say?\"");
        }

        public override void SetDefaults()
        {
            item.width = 86;
            item.height = 90;
            item.scale = 2.0f;
            item.useStyle = 1;
            item.useTime = 40;
            item.useAnimation = item.useTime;
            item.UseSound = SoundID.Item1;
            item.damage = 100;
            item.crit = 5;
            item.knockBack = 5f;
            item.melee = true;
            item.autoReuse = true;
            item.rare = 9;
            item.value = Item.sellPrice(0, 15, 0, 0);
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            int type = mod.ProjectileType<ProjCrusherPillar>();
            int offset = ProjCrusherPillar.TargetDistance; //How far from the target the pillars should spawn
            float shootSpeed = !crit ? 5 : 10; //Pillars crush twice as fast if it's a critical hit

            Projectile.NewProjectile(new Vector2(target.Center.X+offset, target.Center.Y), new Vector2(-shootSpeed, 0f), type, damage * 2, knockBack*0f, player.whoAmI, 0, crit ? 1 : 0); //Right pillar
            Projectile.NewProjectile(new Vector2(target.Center.X-offset, target.Center.Y), new Vector2(+shootSpeed, 0f), type, damage * 2, knockBack*0f, player.whoAmI, 0, crit ? 1 : 0); //Left pillar
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.StaffofEarth);
            recipe.AddIngredient(ItemID.ChlorophyteWarhammer);
            recipe.AddIngredient(ItemID.Marble, 20);
            recipe.AddTile(TileID.WorkBenches);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
