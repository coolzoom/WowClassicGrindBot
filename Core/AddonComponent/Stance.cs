﻿namespace Core
{
    public class Stance
    {
        private readonly int cell;

        private int value;

        public Stance(int cell)
        {
            this.cell = cell;
        }

        public void Update(IAddonDataProvider reader)
        {
            value = reader.GetInt(cell);
        }

        public Form Get(PlayerReader playerReader, PlayerClassEnum playerClass) => value == 0 ? Form.None : playerClass switch
        {
            PlayerClassEnum.Warrior => Form.Warrior_BattleStance + value - 1,
            PlayerClassEnum.Rogue => Form.Rogue_Stealth + value - 1,
            PlayerClassEnum.Priest => Form.Priest_Shadowform + value - 1,
            PlayerClassEnum.Druid => playerReader.Buffs.Prowl() ? Form.Druid_Cat_Prowl : Form.Druid_Bear + value - 1,
            PlayerClassEnum.Paladin => Form.Paladin_Devotion_Aura + value - 1,
            PlayerClassEnum.Shaman => Form.Shaman_GhostWolf + value - 1,
            PlayerClassEnum.DeathKnight => Form.DeathKnight_Blood_Presence + value - 1,
            _ => Form.None
        };

        public static int ToSlot(KeyAction item, PlayerReader playerReader)
        {
            return item.Slot <= ActionBar.MAIN_ACTIONBAR_SLOT
                ? item.Slot + (int)FormToActionBar(playerReader.Class, item.HasFormRequirement ? item.FormEnum : playerReader.Form)
                : item.Slot;
        }

        private static StanceActionBar FormToActionBar(PlayerClassEnum playerClass, Form form)
        {
            switch (playerClass)
            {
                case PlayerClassEnum.Druid:
                    switch (form)
                    {
                        case Form.Druid_Cat:
                            return StanceActionBar.DruidCat;
                        case Form.Druid_Cat_Prowl:
                            return StanceActionBar.DruidCatProwl;
                        case Form.Druid_Bear:
                            return StanceActionBar.DruidBear;
                        case Form.Druid_Moonkin:
                            return StanceActionBar.DruidMoonkin;
                    }
                    break;
                case PlayerClassEnum.Warrior:
                    switch (form)
                    {
                        case Form.Warrior_BattleStance:
                            return StanceActionBar.WarriorBattleStance;
                        case Form.Warrior_DefensiveStance:
                            return StanceActionBar.WarriorDefensiveStance;
                        case Form.Warrior_BerserkerStance:
                            return StanceActionBar.WarriorBerserkerStance;
                    }
                    break;
                case PlayerClassEnum.Rogue:
                    if (form == Form.Rogue_Stealth)
                        return StanceActionBar.RogueStealth;
                    break;
                case PlayerClassEnum.Priest:
                    if (form == Form.Priest_Shadowform)
                        return StanceActionBar.PriestShadowform;
                    break;
            }

            return StanceActionBar.None;
        }
    }
}
