﻿using RimWorld;
using RimWorld.BaseGen;
using System;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace KCSG
{
    internal class SymbolResolver_Settlement : SymbolResolver
    {
        public override void Resolve(ResolveParams rp)
        {
            Map map = BaseGen.globalSettings.map;
            Faction faction = rp.faction ?? Find.FactionManager.RandomEnemyFaction(false, false, true, TechLevel.Undefined);

            if (CurrentGenerationOption.useStructureLayout)
            {
                this.AddHostilePawnGroup(faction, map, rp);

                ResolveParams usl_rp = rp;
                usl_rp.faction = faction;
                BaseGen.symbolStack.Push("kcsg_roomsgenfromstructure", usl_rp, null);

                GenUtils.PreClean(map, rp.rect);
            }
            else
            {
                this.AddHostilePawnGroup(faction, map, rp);

                if (CurrentGenerationOption.settlementLayoutDef.vanillaLikeDefense)
                {
                    int dWidth = (Rand.Bool ? 2 : 4);
                    ResolveParams rp3 = rp;
                    rp3.rect = new CellRect(rp.rect.minX - dWidth, rp.rect.minZ - dWidth, rp.rect.Width + (dWidth * 2), rp.rect.Height + (dWidth * 2));
                    rp3.faction = faction;
                    rp3.edgeDefenseWidth = dWidth;
                    rp3.edgeThingMustReachMapEdge = new bool?(rp.edgeThingMustReachMapEdge ?? true);
                    BaseGen.symbolStack.Push("edgeDefense", rp3, null);
                }

                this.GenerateRooms(CurrentGenerationOption.settlementLayoutDef, map, rp);

                GenUtils.PreClean(map, rp.rect);
            }
        }

        private void AddHostilePawnGroup(Faction faction, Map map, ResolveParams rp)
        {
            Lord singlePawnLord = rp.singlePawnLord ?? LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, rp.rect.CenterCell), map, null);
            TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false);
            ResolveParams resolveParams = rp;
            resolveParams.rect = rp.rect;
            resolveParams.faction = faction;
            resolveParams.singlePawnLord = singlePawnLord;
            resolveParams.pawnGroupKindDef = (rp.pawnGroupKindDef ?? PawnGroupKindDefOf.Settlement);
            resolveParams.singlePawnSpawnCellExtraPredicate = (rp.singlePawnSpawnCellExtraPredicate ?? ((IntVec3 x) => map.reachability.CanReachMapEdge(x, traverseParms)));
            if (resolveParams.pawnGroupMakerParams == null && faction.def.pawnGroupMakers.Any(pgm => pgm.kindDef == PawnGroupKindDefOf.Settlement))
            {
                resolveParams.pawnGroupMakerParams = new PawnGroupMakerParms
                {
                    tile = map.Tile,
                    faction = faction,
                    points = rp.settlementPawnGroupPoints ?? RimWorld.BaseGen.SymbolResolver_Settlement.DefaultPawnsPoints.RandomInRange,
                    inhabitants = true,
                    seed = rp.settlementPawnGroupSeed
                };
            }
            if (faction.def.pawnGroupMakers.Any(pgm => pgm.kindDef == PawnGroupKindDefOf.Settlement)) BaseGen.symbolStack.Push("pawnGroup", resolveParams, null);
        }

        private void GenerateRooms(SettlementLayoutDef sld, Map map, ResolveParams rp)
        {
            DateTime startTime = DateTime.Now;
            Log.Message($"Starting generation - {startTime.ToShortTimeString()}");
            int seed = new Random().Next(0, 100000);
            CurrentGenerationOption.offset = rp.rect.Corners.ElementAt(2);
            CurrentGenerationOption.fullRect = rp.rect;

            CurrentGenerationOption.grid = GridUtils.GenerateGrid(seed, sld, map, out CurrentGenerationOption.vectStruct);

            ResolveParams usl_rp = rp;
            usl_rp.faction = rp.faction;

            BaseGen.symbolStack.Push("kcsg_roomgenfromlist", usl_rp, null);

            BaseGen.symbolStack.Push("kcsg_generateroad", usl_rp, null);

            CurrentGenerationOption.vectors.RemoveAt(0);
            int n = 0;
            Random r = new Random(seed);
            foreach (CustomVector c in CurrentGenerationOption.vectors)
            {
                if (n % 3 == 0)
                {
                    ResolveParams gzp = rp;
                    int x = rp.rect.Corners.ElementAt(2).x,
                        y = rp.rect.Corners.ElementAt(2).z;
                    IntVec3 cell = new IntVec3(x + (int)c.X + CurrentGenerationOption.radius / 2, 0, y - (int)c.Y - CurrentGenerationOption.radius / 2);
                    gzp.rect = CellRect.CenteredOn(cell, r.Next(4, CurrentGenerationOption.radius + 1), r.Next(4, CurrentGenerationOption.radius + 4)).ClipInsideRect(rp.rect);
                    gzp.cultivatedPlantDef = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(t => t.plant != null && !t.plant.cavePlant && t.plant.Harvestable && !t.plant.IsTree).RandomElement();

                    BaseGen.symbolStack.Push("cultivatedPlants", gzp, null);
                }
                n++;
            }

            if (rp.faction.def == FactionDefOf.Empire || (sld.addLandingPad && ModLister.RoyaltyInstalled))
            {
                if (rp.rect.TryFindRandomInnerRect(new IntVec2(9, 9), out CellRect rect, null))
                {
                    ResolveParams resolveParams = rp;
                    resolveParams.rect = rect;
                    BaseGen.symbolStack.Push("landingPad", resolveParams, null);
                    BaseGen.globalSettings.basePart_landingPadsResolved++;
                }
            }

            Log.Message($"Generation stopped - {DateTime.Now.ToShortTimeString()} - Time taken {(DateTime.Now - startTime).TotalMilliseconds} ms - Seed was {seed}.");
        }
    }
}