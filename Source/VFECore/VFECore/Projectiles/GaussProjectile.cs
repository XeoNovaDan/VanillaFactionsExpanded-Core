﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace VFECore
{
	public class GaussProjectile : ExpandableProjectile
	{
		public override int DamageAmount
        {
			get
            {
				var baseDamage = def.projectile.GetDamageAmount(weaponDamageMultiplier);
				var damageMultiplier = 1f;
				damageMultiplier += ((float)hitThings.Count / 10f);
				var damageAmount = (int)(baseDamage / damageMultiplier);
				return damageAmount;
			}
        }

		public override void DoDamage(IntVec3 pos)
		{
			base.DoDamage(pos);
			try
			{
				if (pos != this.launcher.Position && this.launcher.Map != null && GenGrid.InBounds(pos, this.launcher.Map))
				{
					var list = this.launcher.Map.thingGrid.ThingsListAt(pos);
					for (int num = list.Count - 1; num >= 0; num--)
					{
						if (list[num].def != this.def && list[num] != this.launcher && list[num].def != ThingDefOf.Fire && (!(list[num] is Mote) && (!(list[num] is Filth))))
						{
							this.customImpact = true;
							base.Impact(list[num]);
							this.customImpact = false;
						}
					}
				}
			}
			catch { };
		}
	}
}
