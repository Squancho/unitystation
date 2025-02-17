﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Main component for cloning console
/// </summary>
public class CloningConsole : MonoBehaviour
{
	public delegate void ChangeEvent();
	public static event ChangeEvent changeEvent;

	public List<CloningRecord> CloningRecords = new List<CloningRecord>();
	public DNAscanner scanner;

	public void ToggleLock()
	{
		if(scanner && scanner.IsClosed)
		{
			scanner.IsLocked = !scanner.IsLocked;
		}
	}

	public void Scan()
	{
		if(scanner && scanner.occupant)
		{
			var mob = scanner.occupant;
			var uniqueIdentifier = "35562Eb18150514630991";
			for (int i = 0; i < CloningRecords.Count; i++)
			{
				if(uniqueIdentifier == CloningRecords[i].UniqueIdentifier)
				{
					return;
				}
			}
			var name = mob.GetComponent<PlayerScript>().playerName;
			var oxyDmg = mob.bloodSystem.oxygenDamage;
			var burnDmg = mob.GetTotalBurnDamage();
			var toxinDmg = 0;
			var bruteDmg = mob.GetTotalBruteDamage();
			CreateRecord(name, oxyDmg, burnDmg, toxinDmg, bruteDmg, uniqueIdentifier);
		}
	}

	public void CreateRecord(string name, float oxyDmg, float burnDmg, float toxingDmg, float bruteDmg, string uniqueIdentifier)
	{
		int scanID = Random.Range(0, 1000);
		var CRone = new CloningRecord(name, scanID, oxyDmg, burnDmg, toxingDmg, bruteDmg, uniqueIdentifier);
		CloningRecords.Add(CRone);
	}
}

public class CloningRecord
{
	public string Name;
	public string ScanID;
	public float OxyDmg;
	public float BurnDmg;
	public float ToxingDmg;
	public float BruteDmg;
	public string UniqueIdentifier;

	public CloningRecord(string name, int scanID, float oxyDmg, float burnDmg, float toxingDmg, float bruteDmg, string uniqueIdentifier)
	{
		Name = name;
		ScanID = scanID.ToString();
		OxyDmg = oxyDmg;
		BurnDmg = burnDmg;
		ToxingDmg = toxingDmg;
		BruteDmg = bruteDmg;
		UniqueIdentifier = uniqueIdentifier;
	}
}