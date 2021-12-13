> The Azure Tenant Security Solution (AzTS) can be used to obtain visibility to cloud subscriptions and resource configuration across multiple subscriptions in an enterprise environment. 
The AzTS is a logical progression of DevOps Kit which helps us move closer to an implementation of cloud security compliance solution using native security capabilities in Azure platform that are available today. Functionally, it is similar to running AzSK Continuous Assurance (CA) in central-scan mode.

You can know more about AzTS [here](https://github.com/azsk/AzTS-docs).

# [Org Policy Customization](README.md#org-policy-customization)

### [Overview](README.md#Overview)
 - [When and why should I set up org policy?](Readme.md#when-and-why-should-i-setup-org-policy)

### [Setting up org policy](README.md#setting-up-org-policy)
 <!-- - [What happens during org policy setup?](Readme.md#what-happens-during-org-policy-setup)
 - [First-time policy setup - an example](Readme.md#first-time-policy-setup---an-example) -->

### [Modifying and customizing org policy](README.md#modifying-and-customizing-org-policy)
 - [Structure](Readme.md#structure)
 - [Know more about controls](Readme.md#know-more-about-controls)
 - [Steps to extend a control](Readme.md#steps-to-extend-a-control)
### [Common Application of Org Policy Customization](README.common-application-of-org-policy-customization)

------------------------------------------------------

## [Overview](README.md#Overview)

#### When and why should I setup org policy

When you run any scan from the AzTS, it relies on JSON-based policy files to determine various parameters that affect the behavior of the scan it is about to run. These policy files are downloaded 'on the fly' from a policy server. When you run the public version of the AzTS, the policy files are accessed from a CDN endpoint that is managed by the AzTS team. Thus, whenever you run a scan from a vanilla installation, AzTS accesses the CDN endpoint to get the latest policy configuration and runs the scan using it. 

The JSON inside the policy files dictate the behavior of the security scan. This includes things such as:
 - Which set of controls to evaluate?
 - What control set to use as a baseline?
 - What settings/values to use for individual controls? 
 - What messages to display for recommendations? Etc.

Note that the policy files needed for security scans are accessed from the last updated deployed AzTS package. 

While the out-of-box files in the package may be good for limited use, in many contexts you may want to "customize" the behavior of the security scans for your environment. You may want to do things such as: 
(a) enable/disable some controls, 
(b) change control settings to better match specific security policies within your org, 
(c) change various messages,
(d) modify existing control logic
(e) add additional filter criteria for certain regulatory requirements that teams in your org can leverage, etc. 
(f) add new controls to existing service

When faced with such a need, you need a way to create and manage a dedicated policy endpoint customized to the needs of your environment. The organization policy customization setup feature helps you do that in an automated fashion.

In this document, we will look at how to setup an organization-specific policy endpoint, how to make changes 
to and manage the policy files and how to accomplish various common org-specific policy/behavior customizations 
for the AzTS.

## [Setting up org policy](README.md#setting-up-org-policy)

In this section, we will walk through the steps of setting up organization-specific policy customizable AzTS Scanner locally.

> **Note**: You would require at least 'Reader' level access on Subscription and 'Contributor' level access to the LA Workspace, Storage, etc.

Let's Start!

1. Clone [this](https://github.com/azsk/AzTS-Samples) GitHub repository in a new Visual Studio.
2. Go to AzTS_Extended folder and load the AzTS_Extended.sln.
3. Files to update: 
    * In local.settings.json file: 
        ```JSON
            {
            "IsEncrypted": false,
            "Values": {
                "ASPNETCORE_ENVIRONMENT": "Local",
                "AzureWebJobsStorage": "UseDevelopmentStorage=true",
                "FUNCTIONS_WORKER_RUNTIME": "dotnet",
                "APPINSIGHTS_INSTRUMENTATIONKEY": "",
                "AzureStorageSettings__ResourceId": "",
                "LAConfigurations__WorkspaceId": "",
                "LAConfigurations__ResourceId": ""
            }
            } 
        ```          
        1. Application insights collect telemetry data from connected apps and provides Live Metrics, Log Analytics, etc. It has an instrumentation key which we need to configure into our function app i.e. APPINSIGHTS_INSTRUMENTATIONKEY and with this key app insights grab data from our app. Add instrumentation key for Application Insights by entering "APPINSIGHTS_INSTRUMENTATIONKEY"
	    2. Storage Account and Log Analytic Workspace are used to store the scan events, inventory, subscription scan progress details and results.
	        1. Add 'ResourceId' of the Storage Account,
		    2. Add 'WorkspaceId' and 'ResourceId' of the LA Workspace
    * Mention the ID of the subscription to be scanned in Processor.cs, (line 33)
4. Build and Run

This will install the required [NuGet packages](https://www.nuget.org/packages/Microsoft.AzTS.Azure.Scanner/). It will import the dependencies and dynamic linked libraries of the AzTS Scanner to the user's solution along with the following templates:

| Template File | Description 
| ---- | ---- | 
| FeatureNameExt.json <br> [under the ControlConfigurationExt folder] | This file contains the setting of controls of a specific feature. A few meta-data are required for a control to be scanned which are mentioned in detail further ahead.
| FeatureNameControlEvaluatorExt.cs <br> [under the ControlEvaluator folder] | This file is used to override the base control evaluation method.

Next, we will look into how to modify an existing control or add a new control through this setup.

## [Modifying and customizing org policy](README.md#modifying-and-customizing-org-policy)

The typical workflow for all control changes will remain same and will involve the following basic steps:
1. Make modifications to the existing control metadata (Json files).
2. Add or Modify control methods in respective control evaluator files.
3. Build and Run

### [Structure](README.md#structure) 
Before we get started with extending the toolkit, let's understand the structure of the built solution repository. 

        ├───AzTS_Extended
        ├───Connected Services  
        ├───Dependencies
        ├───Properties
        ├───ConfigurationProvider    
        │   ├───ControlConfigurations   
        │   └───RoleDefinitionConfigurations   
        ├───Configurations
        │   ├───LAQueries
        ├───ControlConfigurationExt
        ├───ControlEvaluator


### [Know more about controls](Readme.md#know-more-about-controls)

All our controls inherit from a base class called BaseControlEvaluator which will take care of all the required plumbing from the control evaluation code. Every control will have a corresponding feature json file under the configurations folder. For example, Storage.cs (in the control evaluator folder) has a corresponding Storage.json file under configurations folder. These controls json have a bunch of configuration parameters, that can be controlled by a policy owner, for instance, you can change the recommendation, modify the description of the control suiting your org, change the severity, etc.

Below is the typical schema for each control inside the feature json

```JSON
{
    "ControlID": "Azure_Storage_NetSec_Restrict_Network_Access",   //Human friendly control Id. The format used is Azure_<FeatureName>_<Category>_<ControlName>
    "Description": "Ensure that Firewall and Virtual Network access is granted to a minimal set of trusted origins",  //Description for the control, which is rendered in all the reports it generates (CSV, AI telemetry, emails etc.).
    "Id": "AzureStorage260",   //This is internal ID and should be unique. Since the ControlID can be modified, this internal ID ensures that we have a unique link to all the control results evaluation.
    "ControlSeverity": "Medium", //Represents the severity of the Control. 
    "Automated": "Yes",   //Indicates whether the given control is Manual/Automated.
    "MethodName": "CheckStorageNetworkAccess",  // Represents the Control method that is responsible to evaluate this control. It should be present inside the feature SVT associated with this control.
    "DisplayName": "Ensure that Firewall and Virtual Network access is granted to a minimal set of trusted origins", // Represents human friendly name for the control.
    "Recommendation": "Go to Azure Portal --> your Storage service --> Settings --> Firewalls and virtual networks --> Selected Network. Provide the specific IP address and Virtual Network details that should be allowed to access storage account.",	  //Recommendation typically provides the precise instructions on how to fix this control.
    "Tags": [
        "SDL",
        "TCP",
        "Automated",
        "NetSec",
        "Baseline"
    ], // You can decorate your control with different set of tags, that can be used as filters in scan commands.
    "Enabled": true ,  //Defines whether the control is enabled or not.
    "Rationale": "Restricting access using firewall/virtual network config reduces network exposure of a storage account by limiting access only to expected range/set of clients. Note that this depends on the overall service architecture and may not be possible to implement in all scenarios." //Provides the intent of this control.
}
```

After Schema of the control json, let us look at the corresponding feature 

``` CS
public class StorageControlEvaluator : BaseControlEvaluator
{
    public void CheckStorageNetworkAccess(Resource storage, ControlResult cr)
    {
        // 1. This is where the code logic is placed
        // 2. ControlResult input to this function, which needs to be updated with the verification Result (Passed/Failed/Verify/Manual/Error) based on the control logic
        // 3. Messages that you add to ControlResult variable will be displayed in the detailed log automatically.
        
        if (!string.IsNullOrEmpty(storage.CustomField1))
        {
            // Start with failed state, mark control as Passed if all required conditions are met
            cr.VerificationResult = VerificationResultStatus.Failed;
            cr.ScanSource = ScanResourceType.Reader.ToString();

            // CustomField1 has details about which protocol is supported by Storage for traffic
            var stgDetails = JObject.Parse(storage.CustomField1);
            string strNetworkRuleSet = stgDetails["NetworkRuleSet"].Value<string>();

            if (strNetworkRuleSet.Equals("Deny", StringComparison.OrdinalIgnoreCase))
            {
                // Firewall and Virtual Network restrictions are defined for this storage
                cr.StatusReason = $"Firewall and Virtual Network restrictions are defined for this storage";
                cr.VerificationResult = VerificationResultStatus.Passed;
            }
            else
            {
                // No Firewall and Virtual Network restrictions are defined for this storage
                cr.StatusReason = $"No Firewall and Virtual Network restrictions are defined for this storage";
                cr.VerificationResult = VerificationResultStatus.Failed;
            }
        }

        // 'Else' block not required since CustomField1 is never expected to be null
    }
    .
    .
    .
}
```
<!-- Add a block diagram here to show how the overlay happens -->

### [Steps to extend a control](Readme.md#steps-to-extend-a-control)
1. [**Control JSON:**](README.md#control-json)
    1. Copy _FeatureNameExt.json_ file and rename it accordingly. For example: StorageExt.json
	2. Fill the parameters according to the feature. For example: 
        ``` JSON
        {
            "FeatureName": "Storage",
            "Reference": "aka.ms/azsktcp/storage", // you can find this from the FeatureName.json as well
            "IsMaintenanceMode": false,
        }
        ```
	3. Add the control json with all parameters given in template. The following meta-data are required for a control to be scanned:
        ``` JSON
        "Controls": [
            {
            "ControlID": "",
            "Id": "",
            "Automated": "Yes",
            "MethodName": "",
            "DisplayName": "",
            "Enabled": false
            }
        ]
        ```
        1. For **Id** above: 
            * If it is an existing control that you wish to modify, then use the same ID as used previously. 
            * If it is a new control, then follow a convention of a FeatureName followed by a *four* digit ID number. For example, "Storage1005" can be the ID for a new control implemented in Storage feature.
        2. For **ControlID** above: Initial part of the control ID is pre-populated based on the service/feature and security domain you choose for the control (Azure_FeatureName_SecurityDomain_XXX). Please don't use spaces between words instead use underscore '_' to separate words in control ID. To see some of the examples of existing control IDs please check out this [list](https://github.com/azsk/AzTS-docs/tree/main/Control%20coverage#azure-services-supported-by-azts).
        3. Keep **Enabled** switch to 'Yes' to scan a control.
        4. **DisplayName** is the user friendly name for the control.


    > *Note*:  You can provide additional details/optional settings for the control as listed below.

    |Settings| Description| Examples|
    |-------------|------|---------|
    |Automated| Whether the control is manual or automated| e.g. Yes/No (keep it Yes for policy based controls)|
    |Description| A basic description on what the control is about| e.g. App Service must only be accessible over HTTPS. |
    | Category| Generic security specification of the control.| e.g. Encrypt data in transit |
    |Tags| Labels that denote the control being of a specific type or belonging to a specific domain | For e.g. Baseline, Automated etc.|
    |Control Severity| The severity of the control| e.g. High: Should be remediated as soon as possible. Medium: Should be considered for remediation. Low: Remediation should be prioritized after high and medium.|
    |Control Requirements| Prerequisites for the control.| e.g. Monitoring and auditing must be enabled and correctly configured according to prescribed organizational guidance|
    |Rationale|  Logical intention for the added control | e.g. Auditing enables log collection of important system events pertinent to security. Regular monitoring of audit logs can help to detect any suspicious and malicious activity early and respond in a timely manner.|
    |Recommendations| Steps or guidance on how to remediate non-compliant resources | e.g. Refer https://azure.microsoft.com/en-in/documentation/articles/key-vault-get-started/ for configuring Key Vault and storing secrets |
    |Custom Tags| Tags can be used for filtering and referring controls in the future while reporting| e.g. Production, Phase2 etc. |
    |Control Settings| Settings specific to the control to be provided for the scan | e.g. Required TLS version for all App services in your tenant (Note: For policy based contols this should be empty) |
    |Comments | These comments show up in the changelog for the feature. | e.g. Added new policy based control for App Service |

2. [**Control Evaluator:**](README.md#control-evaluator)
	1. Copy _FeatureNameControlEvaluatorExt.cs_ and rename it accordingly. For example: StorageControlEvaluatorExt.cs
	2. Change the FeatureNameEvaluatorExt and FeatureNameControlEvaluator according to the baseControlEvaluator name (line 13) as shown below.
        ``` CS
        // class FeatureNameEvaluatorExt : FeatureNameControlEvaluator
        class StorageEvaluatorExt : StorageControlEvaluator
        {
            // Add control methods here        
        }
        ```
    3. Add the control method according to the [feature documentation](FeatureCoverage/README.md).

## [Common Application of Org Policy Customization](README.common-application-of-org-policy-customization)

1. Customizing/Changing the default Control Metadata
	- You will be able to achieve the following scenarios using this feature:
		1. Update any of the properties of the control metadata according to your organization. The set of properties that you can modify ranges from ControlScanSource (i.e. change from ASCorReader to Reader) to Tags/CustomTags (i.e. add or remove a certain set of tags for a control). 
		2. Update the baselines for your organization by modifying the tags/custom tags. 
		3. Modify the ASC properties of a control. 
			"AssessmentProperties": {
					"AssessmentNames": [
					"1c5de8e1-f68d-6a17-e0d2-ec259c42768c"
					],
			}
2. Customizing the Control Method:
	- You can update the control logic of any existing or new custom control. The user-defined method will get overlayed on the default control logic. 
3. Adding a new Control for existing feature:
	- Users can add a new custom control of existing features by custom Azure Policy or ASC Assessment. Both can be done leveraging this Org policy customization feature. 
	- On a high level, Users can do so by following the below mentioned steps:
		1. Add control metadata of the control in FeatureNameExt.json file.
		2. If the ControlScanSource is ASC based then add the AssessmentProperties as shown below:
			"AssessmentProperties": {
					"AssessmentNames": [
						"<Add_AssessmentPolicyID>"
					],
			}
		3. If the ControlScanSource is Reader based then implement the control logic in the corresponding Method in the FeatureNameControlEvaluatorExt.cs.
	
4. Adding a new control of new feature (Coming Soon...) 
	
