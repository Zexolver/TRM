using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.GameContent;

namespace RestockPlus
{
    public class RestockSystem : ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (inventoryIndex != -1)
            {
                layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
                    "RestockPlus: Restock Button",
                    delegate
                    {
                        // Show when inventory is open, not in a chest, not talking to NPC
                        if (Main.playerInventory && Main.LocalPlayer.chest == -1 && Main.npcShop == 0)
                        {
                            DrawRestockButton();
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

private void DrawRestockButton()
{
    string text = "Restock"; 
    Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text);

    // FIX: Anchor from the top-left instead of the right side.
    // X = 502 places it just to the right of the vanilla Quick Stack icon.
    // Y = 307 aligns it vertically with the vanilla icon/trash slot area.
    Vector2 buttonPos = new Vector2(502, 307);

    Rectangle buttonRect = new Rectangle((int)buttonPos.X, (int)buttonPos.Y, (int)textSize.X, (int)textSize.Y);
    bool isHovering = buttonRect.Contains(Main.mouseX, Main.mouseY);

    Color textColor = isHovering ? Color.White : new Color(150, 150, 150);
    
    if (isHovering)
    {
        Main.LocalPlayer.mouseInterface = true; 

        if (Main.mouseLeft && Main.mouseLeftRelease)
        {
            SoundEngine.PlaySound(SoundID.Grab);
            PerformRestock(Main.LocalPlayer);
        }
    }

    Utils.DrawBorderString(Main.spriteBatch, text, buttonPos, textColor);
}        private void PerformRestock(Player player)
        {
            float restockRange = 400f; // Vanilla Quick Stack range
            bool playedSound = false;

            for (int c = 0; c < Main.chest.Length; c++)
            {
                Chest chest = Main.chest[c];
                if (chest != null && !Chest.IsLocked(chest.x, chest.y))
                {
                    Vector2 chestPosition = new Vector2(chest.x * 16 + 16, chest.y * 16 + 16);
                    
                    if (player.Distance(chestPosition) <= restockRange)
                    {
                        // Check main inventory (slots 0-49)
                        for (int i = 0; i < 50; i++)
                        {
                            Item pItem = player.inventory[i];
                            
                            // If we have an item and it isn't full
                            if (!pItem.IsAir && pItem.stack < pItem.maxStack)
                            {
                                // Look through the chest for the exact same item
                                for (int j = 0; j < Chest.maxItems; j++)
                                {
                                    Item cItem = chest.item[j];
                                    if (!cItem.IsAir && cItem.type == pItem.type)
                                    {
                                        int spaceLeft = pItem.maxStack - pItem.stack;
                                        int amountToTransfer = Math.Min(spaceLeft, cItem.stack);

                                        pItem.stack += amountToTransfer;
                                        cItem.stack -= amountToTransfer;

                                        if (cItem.stack <= 0)
                                            cItem.TurnToAir();

                                        playedSound = true;

                                        // Sync for multiplayer
                                        if (Main.netMode == NetmodeID.MultiplayerClient)
                                        {
                                            NetMessage.SendData(MessageID.SyncChestItem, -1, -1, null, c, j);
                                        }

                                        // Move to the next player item if this stack is full
                                        if (pItem.stack == pItem.maxStack)
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (playedSound)
            {
                Recipe.FindRecipes();
            }
        }
    }
}
