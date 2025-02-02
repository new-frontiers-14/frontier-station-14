#! /bin/python3

import argparse
import os
import yaml
from typing import List, Dict

from util import get_logger, YamlLoaderIgnoringTags, check_prototype
from config import CONDITIONALLY_ILLEGAL_MATCHES

if __name__ == "__main__":
    # Set up argument parser.
    parser = argparse.ArgumentParser(description="Map prototype usage checker for Frontier Station 14.")
    parser.add_argument(
        "-v", "--verbose",
        action='store_true',
        help="Sets log level to DEBUG if present, spitting out a lot more information. False by default,."
    )
    parser.add_argument(
        "-p", "--prototypes_path",
        help="Directory holding entity prototypes.\nDefault: All entity prototypes in the Frontier Station 14 codebase.",
        type=str,
        nargs="+",  # We accept multiple directories, but need at least one.
        required=False,
        default=[
            "Resources/Prototypes/Entities",  # Upstream
            "Resources/Prototypes/_NF/Entities",  # NF
            "Resources/Prototypes/Nyanotrasen/Entities",  # Nyanotrasen
            "Resources/Prototypes/_DV/Entities",  # DeltaV
        ]
    )
    parser.add_argument(
        "-m", "--map_path",
        help=(f"Map PROTOTYPES or directory of map prototypes to check. Can mix and match."
              f"Default: All maps in the Frontier Station 14 codebase."),
        type=str,
        nargs="+",  # We accept multiple pathspecs, but need at least one.
        required=False,
        default=[
            "Resources/Prototypes/_NF/Maps/Outpost",  # Frontier Outpost
            "Resources/Prototypes/_NF/PointsOfInterest",  # Points of interest
            "Resources/Prototypes/_NF/Shipyard",  # Shipyard ships.
        ]
    )
    parser.add_argument(
        "-w", "--whitelist",
        help="YML file that lists map names and prototypes to allow for them.",
        type=str,  # Using argparse.FileType here upsets os.isfile, we work around this.
        nargs=1,
        required=False,
        default=".github/mapchecker/whitelist.yml"
    )

    # ==================================================================================================================
    # PHASE 0: Parse arguments and transform them into lists of files to work on.
    args = parser.parse_args()

    # Set up logging session.
    logger = get_logger(args.verbose)
    logger.info("MapChecker starting up.")
    logger.debug("Verbosity enabled.")

    # Set up argument collectors.
    proto_paths: List[str] = []
    map_proto_paths: List[str] = []
    whitelisted_protos: Dict[str, List[str]] = dict()
    whitelisted_maps: List[str] = []

    # Validate provided arguments and collect file locations.
    for proto_path in args.prototypes_path:  # All prototype paths must be directories.
        if os.path.isdir(proto_path) is False:
            logger.warning(f"Prototype path '{proto_path}' is not a directory. Continuing without it.")
            continue
        # Collect all .yml files in this directory.
        for root, dirs, files in os.walk(proto_path):
            for file in files:
                if file.endswith(".yml"):
                    proto_paths.append(str(os.path.join(root, file)))
    for map_path in args.map_path:  # All map paths must be files or directories.
        if os.path.isfile(map_path):
            # If it's a file, we just add it to the list.
            map_proto_paths.append(map_path)
        elif os.path.isdir(map_path):
            # If it's a directory, we add all .yml files in it to the list.
            for root, dirs, files in os.walk(map_path):
                for file in files:
                    if file.endswith(".yml"):
                        map_proto_paths.append(os.path.join(root, file))
        else:
            logger.warning(f"Map path '{map_path}' is not a file or directory. Continuing without it.")
            continue

    # Validate whitelist, it has to be a file containing valid yml.
    if os.path.isfile(args.whitelist) is False:
        logger.warning(f"Whitelist '{args.whitelist}' is not a file. Continuing without it.")
    else:
        with open(args.whitelist, "r") as whitelist:
            file_data = yaml.load(whitelist, Loader=YamlLoaderIgnoringTags)
            if file_data is None:
                logger.warning(f"Whitelist '{args.whitelist}' is empty. Continuing without it.")
            else:
                for map_key in file_data:
                    if file_data[map_key] is True:
                        whitelisted_maps.append(map_key)
                    elif file_data[map_key] is False:
                        continue
                    else:
                        whitelisted_protos[map_key] = file_data[map_key]

    # ==================================================================================================================
    # PHASE 1: Collect all prototypes in proto_paths that are suffixed with target suffixes.

    # Set up collectors.
    illegal_prototypes: List[str] = list()
    conditionally_illegal_prototypes: Dict[str, List[str]] = dict()
    for key in CONDITIONALLY_ILLEGAL_MATCHES.keys():  # Ensure all keys have empty lists already, less work later.
        conditionally_illegal_prototypes[key] = list()

    # Collect all prototypes and sort into the collectors.
    for proto_file in proto_paths:
        with open(proto_file, "r") as proto:
            logger.debug(f"Reading prototype file '{proto_file}'.")
            file_data = yaml.load(proto, Loader=YamlLoaderIgnoringTags)
            if file_data is None:
                continue

            for item in file_data:  # File data has blocks of things we need.
                if item["type"] != "entity":
                    continue
                proto_id = item["id"]
                proto_name = item["name"] if "name" in item.keys() else ""
                proto_suffixes = str(item["suffix"]).split(", ") if "suffix" in item.keys() else list()

                check_result = check_prototype(proto_id, proto_name, proto_suffixes)
                if check_result is False:
                    illegal_prototypes.append(proto_id)
                elif check_result is not True:
                    for key in check_result:
                        conditionally_illegal_prototypes[key].append(proto_id)

    # Log information.
    logger.info(f"Collected {len(illegal_prototypes)} illegal prototype matchers.")
    for key in conditionally_illegal_prototypes.keys():
        logger.info(f"Collected {len(conditionally_illegal_prototypes[key])} illegal prototype matchers, whitelisted "
                    f"for shipyard group {key}.")
        for item in conditionally_illegal_prototypes[key]:
            logger.debug(f" - {item}")

    # ==================================================================================================================
    # PHASE 2: Check all maps in map_proto_paths for illegal prototypes.

    # Set up collectors.
    violations: Dict[str, List[str]] = dict()

    # Check all maps for illegal prototypes.
    for map_proto in map_proto_paths:
        with open(map_proto, "r") as map:
            file_data = yaml.load(map, Loader=YamlLoaderIgnoringTags)
            if file_data is None:
                logger.warning(f"Map prototype '{map_proto}' is empty. Continuing without it.")
                continue

            map_name = map_proto  # The map name that will be reported over output.
            map_file_location = None
            shipyard_group = None  # Shipyard group of this map, if it's a shuttle.
            # Shipyard override of this map, in the case it's a custom shipyard shuttle but needs to be treated as a
            # specific group.
            shipyard_override = None

            # FIXME: this breaks down with multiple descriptions in one file.
            for item in file_data:
                if item["type"] == "gameMap":
                    # This yaml entry is the map descriptor. Collect its file location and map name.
                    if "id" in item.keys():
                        map_name = item["id"]
                    map_file_location = item["mapPath"] if "mapPath" in item.keys() else None
                elif item["type"] == "vessel":
                    # This yaml entry is a vessel descriptor!
                    shipyard_group = item["group"] if "group" in item.keys() else None
                    shipyard_override = item["mapchecker_group_override"] if "mapchecker_group_override" in item.keys() else None
                elif item["type"] == "pointOfInterest":
                    shipyard_group = "PointOfInterest"
                    shipyard_override = item["mapchecker_group_override"] if "mapchecker_group_override" in item.keys() else None

            if map_file_location is None:
                # Silently skip. If the map doesn't have a mapPath, it won't appear in game anyways.
                logger.debug(f"Map proto {map_proto} did not specify a map file location. Skipping.")
                continue

            # CHECKPOINT - If the map_name is blanket-whitelisted, skip it, but log a warning.
            if map_name in whitelisted_maps:
                logger.warning(f"Map '{map_name}' (from prototype '{map_proto}') was blanket-whitelisted. Skipping it.")
                continue

            if shipyard_override is not None:
                # Log a warning, indicating the override and the normal group this shuttle belongs to, then set
                # shipyard_group to the override.
                logger.warning(f"Map '{map_name}' (from prototype '{map_proto}') is using mapchecker_group_override. "
                               f"This map will be treated as a '{shipyard_override}' shuttle. (Normally: "
                               f"'{shipyard_group}'))")
                shipyard_group = shipyard_override

            logger.debug(f"Starting checks for '{map_name}' (Path: '{map_file_location}' | Shipyard: '{shipyard_group}')")

            # Now construct a temporary list of all prototype ID's that are illegal for this map based on conditionals.
            conditional_checks = set()  # Make a set of it. That way we get no duplicates.
            for key in conditionally_illegal_prototypes.keys():
                if shipyard_group != key:
                    for item in conditionally_illegal_prototypes[key]:
                        conditional_checks.add(item)
            # Remove the ones that do match, if they exist.
            if shipyard_group is not None and shipyard_group in conditionally_illegal_prototypes.keys():
                for check in conditionally_illegal_prototypes[shipyard_group]:
                    if check in conditional_checks:
                        conditional_checks.remove(check)

            logger.debug(f"Conditional checks for {map_name} after removal of shipyard dups: {conditional_checks}")

            # Now we check the map file for these illegal prototypes. I'm being lazy here and just matching against the
            # entire file contents, without loading YAML at all. This is fine, because this job only runs after
            # Content.YamlLinter runs. TODO: It does not.
            with open("Resources" + map_file_location, "r") as map_file:
                map_file_contents = map_file.read()
                for check in illegal_prototypes:
                    # Wrap in 'proto: ' and '\n' here, to ensure we only match actual prototypes, not 'part of word'
                    # prototypes. Example: SignSec is a prefix of SignSecureMed
                    if 'proto: ' + check + '\n' in map_file_contents:
                        if violations.get(map_name) is None:
                            violations[map_name] = list()
                        violations[map_name].append(check)
                for check in conditional_checks:
                    if 'proto: ' + check + '\n' in map_file_contents:
                        if violations.get(map_name) is None:
                            violations[map_name] = list()
                        violations[map_name].append(check)

    # ==================================================================================================================
    # PHASE 3: Filtering findings and reporting.
    logger.debug(f"Violations aggregator before whitelist processing: {violations}")

    # Filter out all prototypes that are whitelisted.
    for key in whitelisted_protos.keys():
        if violations.get(key) is None:
            continue

        for whitelisted_proto in whitelisted_protos[key]:
            if whitelisted_proto in violations[key]:
                violations[key].remove(whitelisted_proto)

    logger.debug(f"Violations aggregator after whitelist processing: {violations}")

    # Some maps had all their violations whitelisted. Remove them from the count.
    total_map_violations = len([viol for viol in violations.keys() if len(violations[viol]) > 0])

    # Report findings to output, on the ERROR loglevel, so they stand out in Github actions output.
    if total_map_violations > 0:
        logger.error(f"Found {total_map_violations} maps with illegal prototypes.")
        for key in violations.keys():
            if len(violations[key]) == 0:
                # If the map has no violations at this point, it's because all of its violations were whitelisted.
                # Don't include them in the report.
                continue

            logger.error(f"Map '{key}' has {len(violations[key])} illegal prototypes.")
            for violation in violations[key]:
                logger.error(f" - {violation}")
    else:
        logger.info("No illegal prototypes found in any maps.")

    logger.info(f"MapChecker finished{' with errors' if total_map_violations > 0 else ''}.")
    if total_map_violations > 0:
        exit(1)
    else:
        exit(0)
