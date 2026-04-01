#pragma once
#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "SocketSubsystem.h"
#include <Interfaces/IPv4/IPv4Address.h>
#include "IPAddress.h"
#include "Interfaces/IPv4/IPv4Endpoint.h"
#include "Common/UdpSocketBuilder.h"
#include "Sockets.h"
#include "UnrealNetworkObject.h"
#include "NetManager.generated.h"

class UUnrealNetworkObject;

UCLASS()
class MYPROJECT_API ANetManager : public AActor {
	GENERATED_BODY()
	
public:	
	FIPv4Endpoint serverep;
	FIPv4Endpoint localep;
	FIPv4Address address;
	FSocket* socket;

	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Network")
	TSubclassOf<AActor> otherPlayerAvatar;

	// Sets default values for this actor's properties
	ANetManager();

	//UFUNCTION(BlueprintCallable, Category = "Network")
	//static ANetManager* GetNetManager();
	static TArray<UUnrealNetworkObject*>& GetLocalNetObjects();

	static ANetManager* netManagerInstance;

	void AddNetworkObject(UUnrealNetworkObject* networkObject);

protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;
	virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;

public:	
	// Called every frame
	virtual void Tick(float DeltaTime) override;

	void SendTransformDataPacket(const FString& PacketData);

	static TArray<UUnrealNetworkObject*> localNetObjects;
	static TArray<UUnrealNetworkObject*> globalNetObjects;

	
};