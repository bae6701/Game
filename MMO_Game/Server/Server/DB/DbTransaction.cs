using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DB
{
    public partial class DbTransaction : JobSerializer
    {
        public static DbTransaction Instance { get; } = new DbTransaction();

        // Me (GameRoom) -> You (Db) -> Me (GameRoom)
        public static void SavePlayerStatus_AllInOne(Player player, GameRoom room)
        {
            if (player == null || room == null)
                return;

            // Me (GameRoom)
            PlayerDb playerDb = new PlayerDb();
            playerDb.PlayerDbId = player.PlayerDbId;
            playerDb.Hp = player.Stat.Hp;

            // YOU (Db)
            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Entry(playerDb).State = EntityState.Unchanged;
                    db.Entry(playerDb).Property(nameof(PlayerDb.Hp)).IsModified = true;
                    bool success = db.SaveChangesEx();
                    if (success)
                    {

                    }
                }
            });          
        }
        
        // Me (GameRoom)
        public static void SavePlayerStatus_Step1(Player player, GameRoom room)
        {
            if (player == null || room == null)
                return;

            // Me (GameRoom)
            PlayerDb playerDb = new PlayerDb();
            playerDb.PlayerDbId = player.PlayerDbId;
            playerDb.Hp = player.Stat.Hp;
            Instance.Push<PlayerDb, GameRoom>(SavePlayerStatus_Step2, playerDb, room);
        }
        
        // You (Db)
        public static void SavePlayerStatus_Step2(PlayerDb playerDb, GameRoom room)
        {
            using (AppDbContext db = new AppDbContext())
            {
                db.Entry(playerDb).State = EntityState.Unchanged;
                db.Entry(playerDb).Property(nameof(PlayerDb.Hp)).IsModified = true;
                bool success = db.SaveChangesEx();
                if (success)
                {
                    room.Push(SavePlayerStatus_Step3, playerDb.Hp);
                }
            }
        }
        
        // Me
        public static void SavePlayerStatus_Step3(int hp)
        { 
        }

        public static void RewardPlayer(Player player, RewardData rewardData, GameRoom room)
        {
            if (player == null || rewardData == null || room == null)
                return;

            // 1) DB에다가 저장 요철
            // 2) DB저장 OK
            // 3) 메모리에 적용
            int? slot = player.Inven.GetEmptySlot();
            if (slot == null)
                return;

            ItemDb itemDb = new ItemDb()
            {
                TemplateId = rewardData.itemId,
                Count = rewardData.count,
                Slot = slot.Value,
                OwnerDbId = player.PlayerDbId
            };

            // YOU (Db)
            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Items.Add(itemDb);
                    bool success = db.SaveChangesEx();
                    if (success)
                    {
                        room.Push(() =>
                        {
                            Item newItem = Item.MakeItem(itemDb);
                            player.Inven.Add(newItem);

                            {
                                S_AddItem itemPacket = new S_AddItem();
                                ItemInfo itemInfo = new ItemInfo();
                                itemInfo.MergeFrom(newItem.Info);
                                itemPacket.Items.Add(itemInfo);

                                player.Session.Send(itemPacket);
                            }
                        });
                    }
                }
            });
        }
    }
}
