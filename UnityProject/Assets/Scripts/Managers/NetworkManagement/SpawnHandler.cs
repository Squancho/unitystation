﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public static class SpawnHandler
{
	private static readonly CustomNetworkManager networkManager = CustomNetworkManager.Instance;

	public static void SpawnViewer(NetworkConnection conn, short playerControllerId, JobType jobType = JobType.NULL)
	{
		GameObject joinedViewer = Object.Instantiate(networkManager.playerPrefab);
		NetworkServer.AddPlayerForConnection(conn, joinedViewer, 0);
	}

	public static void SpawnDummyPlayer(JobType jobType = JobType.NULL)
	{
		GameObject dummyPlayer = CreatePlayer(jobType);
		TransferPlayer(null, 0, dummyPlayer, null, EVENT.PlayerSpawned, null);
	}

	public static void ClonePlayer(NetworkConnection conn, short playerControllerId, JobType jobType, CharacterSettings characterSettings, GameObject oldBody, GameObject spawnSpot)
	{
		GameObject player = CreateMob(jobType, spawnSpot, CustomNetworkManager.Instance.humanPlayerPrefab);
		TransferPlayer(conn, playerControllerId, player, oldBody, EVENT.PlayerSpawned, characterSettings);
	}

	public static void RespawnPlayer(NetworkConnection conn, short playerControllerId, JobType jobType, CharacterSettings characterSettings, GameObject oldBody)
	{
		GameObject player = CreatePlayer(jobType);
		TransferPlayer(conn, playerControllerId, player, oldBody, EVENT.PlayerSpawned, characterSettings);
		var playerScript = player.GetComponent<PlayerScript>();
		playerScript.mind = new Mind();
		playerScript.mind.body = playerScript;
	}

	public static GameObject SpawnPlayerGhost(NetworkConnection conn, short playerControllerId, GameObject oldBody, CharacterSettings characterSettings)
	{
		var jobType = oldBody.GetComponent<PlayerScript>().JobType;
		GameObject ghost = CreateMob(jobType, oldBody, CustomNetworkManager.Instance.ghostPrefab);
		TransferPlayer(conn, playerControllerId, ghost, oldBody, EVENT.GhostSpawned, characterSettings);
		return ghost;
	}

	/// <summary>
	/// Connects a client to a character or redirects a logged out ConnectedPlayer.
	/// </summary>
	/// <param name="conn">The client's NetworkConnection. If logged out the playerlist will return an invalid connectedplayer</param>
	/// <param name="playerControllerId">ID of the client player to be transfered. If logged out it's empty.</param>
	/// <param name="newBody">The character gameobject to be transfered into.</param>
	/// <param name="oldBody">The old body of the character.</param>
	/// <param name="eventType">Event type for the player sync.</param>
	public static void TransferPlayer(NetworkConnection conn, short playerControllerId, GameObject newBody, GameObject oldBody, EVENT eventType, CharacterSettings characterSettings)
	{
		var connectedPlayer = PlayerList.Instance.Get(conn);
		if (connectedPlayer == ConnectedPlayer.Invalid) //this isn't an online player
		{
			PlayerList.Instance.UpdateLoggedOffPlayer(newBody, oldBody);
			NetworkServer.Spawn(newBody);
		}
		else
		{
			PlayerList.Instance.UpdatePlayer(conn, newBody);
			NetworkServer.ReplacePlayerForConnection(conn, newBody, playerControllerId);
			TriggerEventMessage.Send(newBody, eventType);
		}
		var playerScript = newBody.GetComponent<PlayerScript>();
		if (playerScript.PlayerSync != null)
		{
			playerScript.PlayerSync.NotifyPlayers(true);
		}

		// If the player is inside a container, send a ClosetHandlerMessage.
		// The ClosetHandlerMessage will attach the container to the transfered player.
		var playerObjectBehavior = newBody.GetComponent<ObjectBehaviour>();
		if (playerObjectBehavior && playerObjectBehavior.parentContainer)
		{
			ClosetHandlerMessage.Send(newBody, playerObjectBehavior.parentContainer.gameObject);
		}

		CustomNetworkManager.Instance.SyncPlayerData(newBody);
		if(characterSettings != null)
		{
			playerScript.ServerSetCharacterSettings(characterSettings);
		}
	}

	private static GameObject CreateMob(JobType jobType, GameObject spawnSpot, GameObject mobPrefab)
	{
		var registerTile = spawnSpot.GetComponent<RegisterTile>();

		Transform parentTransform;
		NetworkInstanceId parentNetId;
		Vector3Int spawnPosition;

		if ( registerTile ) //spawnSpot is someone's corpse
		{
			var objectLayer = registerTile.layer;
			parentNetId = objectLayer.GetComponentInParent<NetworkIdentity>().netId;
			parentTransform = objectLayer.transform;
			spawnPosition = spawnSpot.GetComponent<ObjectBehaviour>().AssumedLocation().RoundToInt();
		}
		else //spawnSpot is a Spawnpoint object
		{
			spawnPosition = spawnSpot.transform.position.CutToInt();

			var matrixInfo = MatrixManager.AtPoint( spawnPosition, true );
			parentNetId = matrixInfo.NetId;
			parentTransform = matrixInfo.Objects;
		}

		GameObject newMob = Object.Instantiate(mobPrefab, spawnPosition, Quaternion.identity, parentTransform);
		var playerScript = newMob.GetComponent<PlayerScript>();

		playerScript.JobType = jobType;
		playerScript.registerTile.ParentNetId = parentNetId;

		return newMob;
	}

	private static GameObject CreatePlayer(JobType jobType)
	{
		GameObject playerPrefab = CustomNetworkManager.Instance.humanPlayerPrefab;

		Transform spawnTransform = GetSpawnForJob(jobType);

		GameObject player;

		if (spawnTransform != null)
		{
			player = CreateMob( jobType, spawnTransform.gameObject, playerPrefab );
		}
		else
		{
			player = Object.Instantiate(playerPrefab);
			player.GetComponent<PlayerScript>().JobType = jobType;
		}

		return player;
	}

	private static Transform GetSpawnForJob(JobType jobType)
	{
		if (jobType == JobType.NULL)
		{
			return null;
		}

		List<SpawnPoint> spawnPoints = networkManager.startPositions.Select(x => x.GetComponent<SpawnPoint>())
			.Where(x => x.JobRestrictions.Contains(jobType)).ToList();

		return spawnPoints.Count == 0 ? null : spawnPoints.PickRandom().transform;
	}


}