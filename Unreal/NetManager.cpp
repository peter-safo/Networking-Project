// Fill out your copyright notice in the Description page of Project Settings.

#include "NetManager.h"
#include "UnrealNetworkObject.h"
#include "Kismet/GameplayStatics.h"

TArray<UUnrealNetworkObject*> ANetManager::localNetObjects;
TArray<UUnrealNetworkObject*> ANetManager::globalNetObjects;
ANetManager* ANetManager::netManagerInstance = nullptr;

// Sets default values
ANetManager::ANetManager()
{
 	// Set this actor to call Tick() every frame
	PrimaryActorTick.bCanEverTick = true;

	// Check if the singleton is null pointer
	if (netManagerInstance == nullptr) {
		// Set the singleton variable to this instance of the class
		netManagerInstance = this;
	}



	FString ip = "127.0.0.1";
	uint16 sPort = 9050; // Server port
	uint16 cPort = 9051; // Client ports
	FIPv4Address serverIP(127, 0, 0, 1);
	//FIPv4Address::Parse(ip, address);
	serverep = FIPv4Endpoint(serverIP, sPort);

	localep = FIPv4Endpoint(FIPv4Address::Any, 0);

	socket = FUdpSocketBuilder("UDPSocket").BoundToEndpoint(localep);

	if (socket) {
		UE_LOG(LogTemp, Log, TEXT("Client complete"));

		// Converting a string to a byte array
		FString myMessage = "I'm an Unreal Client - Hi!";
		TCHAR* charMessage = myMessage.GetCharArray().GetData();
		uint8* array = (uint8*)TCHAR_TO_UTF8(charMessage);
		int32 arrayLength = FCString::Strlen(charMessage);
		int32 bytesSentAmount;
		UE_LOG(LogTemp, Warning, TEXT("Array size %i"), arrayLength);
		UE_LOG(LogTemp, Log, TEXT("Server ep: %s"), *serverep.ToInternetAddr()->ToString(true));

		socket->SendTo(array, arrayLength, bytesSentAmount, *serverep.ToInternetAddr());
		UE_LOG(LogTemp, Log, TEXT("Sent message"));
	}
	else {
		UE_LOG(LogTemp, Error, TEXT("Issue with socket"));
	}
}



TArray<UUnrealNetworkObject*>& ANetManager::GetLocalNetObjects()
{
	return localNetObjects;
}


void ANetManager::AddNetworkObject(UUnrealNetworkObject* networkObject)
{
	// Add the incoming object into the local network object list

	globalNetObjects.Add(networkObject);

	// Check if the object has a global ID of 0 AND if it is locally owned
	if (networkObject->globalID == 0 && networkObject->isLocallyOwned)
	{
		FString message = "Requesting unique network ID for object:" + FString::FromInt(networkObject->localID);
		TCHAR* charMessage = message.GetCharArray().GetData();
		uint8* array = (uint8*)TCHAR_TO_UTF8(charMessage);
		int32 arrayLength = FCString::Strlen(charMessage);
		int32 bytesCount;

		
		if (socket)
		{
			UE_LOG(LogTemp, Warning, TEXT("Message to send: %s"), *message);
			socket->SendTo(array, arrayLength, bytesCount, *serverep.ToInternetAddr());
		}
		else {
			UE_LOG(LogTemp, Error, TEXT("Issue with socket on add object"));
		}

	}
}


// Called when the game starts or when spawned
void ANetManager::BeginPlay()
{
	Super::BeginPlay();


}


void ANetManager::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
	Super::EndPlay(EndPlayReason);

	// Set the singleton variable to a null pointer
	netManagerInstance = nullptr;

	if(socket)
		socket->Close();
	ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->DestroySocket(socket);
	UE_LOG(LogTemp, Log, TEXT("End play"));

	localNetObjects.Empty();
	globalNetObjects.Empty();
}


// Called every frame
void ANetManager::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);


	uint32 sizeData;
	while (socket->HasPendingData(sizeData))
	{
		

		//uint8* dataArray = new uint8[sizeData];
		TArray<uint8> dataArray;
		dataArray.AddUninitialized(sizeData);
		int32 dataRead;
		TSharedPtr<FInternetAddr> targetAddr = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
		bool success = socket->RecvFrom(dataArray.GetData(), sizeData, dataRead, *targetAddr);
		if (success)
		{
			// Convert the received data to FString
			FString receivedMessage = FString(UTF8_TO_TCHAR(dataArray.GetData()));
			UE_LOG(LogTemp, Log, TEXT("Received %s"), *receivedMessage);

			// Check if the message contains the packet type string
			if (receivedMessage.Contains(TEXT("UID")))
			{
				// Parse the received message
				TArray<FString> parseString;
				const TCHAR* delims[] = { TEXT(":"), TEXT(";") };
				receivedMessage.ParseIntoArray(parseString, delims, 2);

				// Extract the ID to check
				int IdCheck = FCString::Atoi(*parseString[3]);
				UE_LOG(LogTemp, Log, TEXT("ID check: %d"), IdCheck);

				int parsedLocal = FCString::Atoi(*parseString[1]);

				// Iterate over global network objects
				for (int i = 0; i < globalNetObjects.Num(); i++)
				{
					if (globalNetObjects[i]->localID == parsedLocal)
					{
						globalNetObjects[i]->globalID = IdCheck;
					}
				}
			}

			if (receivedMessage.Contains(TEXT("PositionRotationPacket")))
			{
				TArray<FString> parseString;
				const TCHAR* delims[] = { TEXT(",") };
				receivedMessage.ParseIntoArray(parseString, delims, 1);

				// Extract the ID to check
				int IdCheck = FCString::Atoi(*parseString[1]);
				UE_LOG(LogTemp, Log, TEXT("ID check: %d"), IdCheck);

				bool isFound = false;

				for (int i = 0; i < globalNetObjects.Num(); i++)
				{
					if (globalNetObjects[i]->globalID == IdCheck || globalNetObjects[i]->globalID == 0)
					{
						isFound = true;
						if (globalNetObjects[i]->isLocallyOwned != true)
						{
							globalNetObjects[i]->FromPacket(receivedMessage);
							//UE_LOG(LogTemp, Log, TEXT("Message: %s"), receivedMessage);
						}
					}
				}

				if (!isFound)
				{
					AActor* actor;
					actor = GetWorld()->SpawnActor<AActor>(otherPlayerAvatar, FVector::ZeroVector, FRotator::ZeroRotator);
					actor->FindComponentByClass<UUnrealNetworkObject>()->globalID = IdCheck;
					actor->FindComponentByClass<UUnrealNetworkObject>()->FromPacket(receivedMessage);
				}
			}
		}
		else
		{
			UE_LOG(LogTemp, Error, TEXT("Issue"));
		}
	}

	for (int i = 0; i < localNetObjects.Num(); i++)
	{
		if (localNetObjects[i]->isLocallyOwned && localNetObjects[i]->globalID != 0) 
		{
			FString tempString = localNetObjects[i]->ToPacket();
			TCHAR* message = tempString.GetCharArray().GetData();
			uint8* array = (uint8*)TCHAR_TO_UTF8(message);
			int32 arrayLength = FCString::Strlen(message);
			int32 bytesSent;
			socket->SendTo(array, arrayLength, bytesSent, *serverep.ToInternetAddr());
		}
	}
}
