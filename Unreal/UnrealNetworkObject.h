// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "UnrealNetworkObject.generated.h"


UCLASS( ClassGroup=(Custom), meta=(BlueprintSpawnableComponent) )
class MYPROJECT_API UUnrealNetworkObject : public UActorComponent
{
	GENERATED_BODY()

public:	
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Network")
	bool isLocallyOwned;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Network")
	int32 globalID;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Network")
	int32 localID;

	static int32 lastLocalID;
	// Sets default values for this component's properties
	UUnrealNetworkObject();

	// Called when the game starts
	virtual void BeginPlay() override;

	// Called every frame
	virtual void TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction * ThisTickFunction) override;

	int32 GetGlobalID() const { return globalID; }
	void SetGlobalID(int32 newGlobalID) { globalID = newGlobalID; }
	int32 GetLocalID() const { return localID; }
	bool IsLocallyOwned() const { return isLocallyOwned;}

	FString ToPacket();
	void FromPacket(FString packetData);


private:	

		
};
