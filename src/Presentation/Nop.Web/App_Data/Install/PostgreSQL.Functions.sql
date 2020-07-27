CREATE OR REPLACE FUNCTION "FullText_IsSupported"()
	RETURNS Boolean
	LANGUAGE 'plpgsql'
	IMMUTABLE
	SECURITY INVOKER
AS
$BODY$
BEGIN
	RETURN True;
END;
$BODY$;

CREATE OR REPLACE FUNCTION "FullText_Disable"() RETURNS void
	LANGUAGE 'plpgsql'
	VOLATILE
	SECURITY INVOKER
AS
$BODY$
BEGIN
   DROP TEXT SEARCH CONFIGURATION nopcommerce;
EXCEPTION
   WHEN undefined_object THEN
END;
$BODY$;

CREATE OR REPLACE FUNCTION "FullText_Enable"() RETURNS void
	LANGUAGE 'plpgsql'
	VOLATILE
	SECURITY INVOKER
AS
$BODY$
BEGIN
   CREATE TEXT SEARCH CONFIGURATION nopcommerce (COPY = pg_catalog.simple);
EXCEPTION
   WHEN unique_violation THEN
END;
$BODY$;

CREATE OR REPLACE FUNCTION "DeleteGuests"("OnlyWithoutShoppingCart" boolean,
                                          "CreatedFromUtc" timestamp with time zone,
                                          "CreatedToUtc" timestamp with time zone,
                                          OUT "TotalRecordsDeleted" integer)
    RETURNS integer
    LANGUAGE 'plpgsql'
    VOLATILE
AS
$BODY$
BEGIN
   CREATE TEMP TABLE tmp_guests (customerid int);
   INSERT into tmp_guests (customerid)
	SELECT c."Id"
	FROM "Customer" c
		LEFT JOIN "ShoppingCartItem" sci ON sci."CustomerId" = c."Id"
		INNER JOIN (
			--guests only
			SELECT ccrm."Customer_Id"
			FROM "Customer_CustomerRole_Mapping" ccrm
				INNER JOIN "CustomerRole" cr ON cr."Id" = ccrm."CustomerRole_Id"
			WHERE cr."SystemName" = 'Guests'
		) g ON g."Customer_Id" = c."Id"
		LEFT JOIN "Order" o ON o."CustomerId" = c."Id"
		LEFT JOIN "BlogComment" bc ON bc."CustomerId" = c."Id"
		LEFT JOIN "NewsComment" nc ON nc."CustomerId" = c."Id"
		LEFT JOIN "ProductReview" pr ON pr."CustomerId" = c."Id"
		LEFT JOIN "ProductReviewHelpfulness" prh ON prh."CustomerId" = c."Id"
		LEFT JOIN "PollVotingRecord" pvr ON pvr."CustomerId" = c."Id"
		LEFT JOIN "Forums_Topic" ft ON ft."CustomerId" = c."Id"
		LEFT JOIN "Forums_Post" fp ON fp."CustomerId" = c."Id"
	WHERE 1 = 1
		--no orders
		AND (o."Id" is null)
		--no blog comments
		AND (bc."Id" is null)
		--no news comments
		AND (nc."Id" is null)
		--no product reviews
		AND (pr."Id" is null)
		--no product reviews helpfulness
		AND (prh."Id" is null)
		--no poll voting
		AND (pvr."Id" is null)
		--no forum topics
		AND (ft."Id" is null)
		--no forum topics
		AND (fp."Id" is null)
		--no system accounts
		AND (c."IsSystemAccount" = False)
		--created from
		AND (("CreatedFromUtc" is null) OR (c."CreatedOnUtc" > "CreatedFromUtc"))
		--created to
		AND (("CreatedToUtc" is null) OR (c."CreatedOnUtc" < "CreatedToUtc"))
		--shopping cart items
		AND ("OnlyWithoutShoppingCart" OR (sci."Id" is null));

    --delete guests
	DELETE from "Customer" WHERE "Id" IN (SELECT customerid FROM tmp_guests);

	--delete attributes
	DELETE FROM "GenericAttribute"
    WHERE
		"EntityId" IN (SELECT tmp_guests.customerid FROM tmp_guests)
		AND ("KeyGroup" = N'Customer');

	--total records
	SELECT COUNT(*) FROM tmp_guests into "TotalRecordsDeleted";

	DROP TABLE tmp_guests;
END;

$BODY$;

CREATE OR REPLACE FUNCTION "ProductTagCountLoadAll"(
	StoreId 				int,
	AllowedCustomerRoleIds	text	--a list of customer role IDs (comma-separated list) for which a product should be shown (if a subject to ACL)
)
RETURNS "TableFunctionResult"
SECURITY INVOKER
IMMUTABLE
LANGUAGE 'plpgsql'
as $BODY$
    DECLARE
        "Entities" json;
        "TotalRecords" int;
    BEGIN
        "Entities" :=(SELECT json_agg(tc)
        FROM (SELECT pt."Id" as "ProductTagId", COUNT(p."Id") as "ProductCount"
        FROM "ProductTag" pt
        LEFT JOIN "Product_ProductTag_Mapping" pptm ON pt."Id" = pptm."ProductTag_Id"
        LEFT JOIN "Product" p ON pptm."Product_Id" = p."Id"
        WHERE
            not p."Deleted"
            AND p."Published"
            AND (StoreId = 0 or (not p."LimitedToStores" OR EXISTS (
                SELECT 1 FROM "StoreMapping" sm
                WHERE sm."EntityId" = p."Id" AND sm."EntityName" = 'Product' and sm."StoreId" = 1
                )))
            AND (LENGTH(AllowedCustomerRoleIds) = 0 or (not p."SubjectToAcl"
                OR EXISTS (
                        select 1
                        from "AclRecord" as acl
                        where acl."CustomerRoleId" = ANY (string_to_array(AllowedCustomerRoleIds, ',')::integer[])
                            and acl."EntityId" = p."Id" AND acl."EntityName" = 'Product')
                ))
        GROUP BY pt."Id"
        ORDER BY pt."Id") as tc);

        GET DIAGNOSTICS "TotalRecords" := row_count;

        RETURN ROW("Entities", ''::text, "TotalRecords")::"TableFunctionResult";
    END;
$BODY$;

CREATE OR REPLACE FUNCTION "ProductLoadAllPaged"(
		"CategoryIds"										text,				--a list of category IDs (comma-separated list). e.g. 1,2,3
		"ManufacturerId"									int,
		"StoreId"											int,
		"VendorId"											int,
		"WarehouseId"										int,
		"ProductTypeId"										int, 				--product type identifier, null - load all products
		"VisibleIndividuallyOnly" 							bool, 				--0 - load all products , 1 - "visible individually" only
		"MarkedAsNewOnly"									bool, 				--0 - load all products , 1 - "marked as new" only
		"ProductTagId"										int,
		"FeaturedProducts"									bool,				--0 featured only , 1 not featured only, null - load all products
		"PriceMin"											decimal(18, 4),
		"PriceMax"											decimal(18, 4),
		"Keywords"											text,
		"SearchDescriptions" 								bool, 				--a value indicating whether to search by a specified "keyword" in product descriptions
		"SearchManufacturerPartNumber" 						bool, 				-- a value indicating whether to search by a specified "keyword" in manufacturer part number
		"SearchSku"											bool, 				--a value indicating whether to search by a specified "keyword" in product SKU
		"SearchProductTags"  								bool, 				--a value indicating whether to search by a specified "keyword" in product tags
		"UseFullTextSearch"  								bool,
		"FullTextMode"										int, 				--0 - using CONTAINS with <prefix_term>, 5 - using CONTAINS and OR with <prefix_term>, 10 - using CONTAINS and AND with <prefix_term>
		"FilteredSpecs"										text,				--filter by specification attribute options (comma-separated list of IDs). e.g. 14,15,16
		"LanguageId"										int,
		"OrderBy"											int, 				--0 - position, 5 - Name: A to Z, 6 - Name: Z to A, 10 - Price: Low to High, 11 - Price: High to Low, 15 - creation date
		"AllowedCustomerRoleIds"							text,				--a list of customer role IDs (comma-separated list) for which a product should be shown (if a subject to ACL)
		"PageIndex"											int,
		"PageSize"											int,
		"ShowHidden"										bool,
		"OverridePublished"									bool, 				--null - process "Published" property according to "showHidden" parameter, true - load only "Published" products, false - load only "Unpublished" products
		"LoadFilterableSpecificationAttributeOptionIds" 	bool
) RETURNS "TableFunctionResult"
    SECURITY INVOKER
	VOLATILE
    LANGUAGE 'plpgsql'
as $BODY$
DECLARE
    "sql_orderby" text;
    "SearchKeywords" bool := false;
    sql_command text := '';
    sql_filterableSpecs text := '';
    keywords text := trim(COALESCE("Keywords", ''));
    original_keywords text := keywords;
    concat_term text;
    fulltext_keywords text := '';
    str_index int := 0;
    find_in_categories bool := array_length(string_to_array("CategoryIds", ','), 1) is not null;
    find_in_customer_roles bool := array_length(string_to_array("AllowedCustomerRoleIds", ','), 1) is not null;
    find_in_specs bool := array_length(string_to_array("FilteredSpecs", ','), 1) is not null;
    filterable_specs int[];
    filterable_specs_count int := 0;
    "Products" json;
    "FilterableSpecificationAttributeOptionIds" text;
    "TotalRecords" int;

BEGIN
    /* Products that filtered by keywords */
	CREATE TEMP TABLE "KeywordProducts"
	(
		"ProductId" int NOT NULL
	);

    IF length(keywords) > 0 then
		"SearchKeywords" := true;
        IF "UseFullTextSearch" then
			--remove wrong chars (' ")
			keywords := REPLACE(keywords, '''', '');
			keywords := REPLACE(keywords, '"', '');
            keywords := ' ' || keywords;

            IF "FullTextMode" = 0 then
				keywords := ' "' || keywords || '*" ';
			else
				--5 - using CONTAINS and OR with <prefix_term>
				--10 - using CONTAINS and AND with <prefix_term>

				--clean multiple spaces
                WHILE position('  ' in quote_nullable(keywords)) > 0 LOOP
					keywords := REPLACE(keywords, '  ', ' ');
				end LOOP;

				IF "FullTextMode" = 5 then --5 - using CONTAINS and OR with <prefix_term>
					concat_term := ' | ';
				END if;
				IF "FullTextMode" = 10 then --10 - using CONTAINS and AND with <prefix_term>
					concat_term := ' & ';
				END if;

                --now let's build search string
				str_index := position(' ' in quote_literal(keywords));

                -- if index = 0, then only one field was passed
				IF(str_index = 0) then
					fulltext_keywords := format(' "%L*" ', keywords);
				ELSE
					fulltext_keywords := replace(keywords, ' ', concat_term);
                end if;
                keywords := fulltext_keywords;
            end if;
        end if;

        --product name
		sql_command := '
		INSERT INTO "KeywordProducts" (ProductId)
		SELECT p."Id"
		FROM "Product" p
		WHERE';

		IF "UseFullTextSearch" then
			sql_command := sql_command || 'to_tsvector(p."Name") @@ to_tsquery($1) ';
		ELSE
			sql_command := sql_command || 'position(p.Name in $1) > 0';
        end if;

        --localized product name
		sql_command := sql_command || format('
		UNION
		SELECT lp.EntityId
		FROM LocalizedProperty lp
		WHERE
			lp.LocaleKeyGroup = %L
			AND lp.LanguageId = %L
			AND lp.LocaleKey = %L', 'Product', "LanguageId", 'Name');

		IF "UseFullTextSearch" then
			sql_command := sql_command || ' AND to_tsvector(lp.LocaleValue) @@ to_tsquery($1) ';
		ELSE
			sql_command := sql_command || ' AND position(lp.LocaleValue in $1) > 0 ';
		end if;

        if "SearchDescriptions" then
			--product short description
			sql_command := sql_command || '
			UNION
			SELECT "p."Id""
			FROM "Product" p
			WHERE ';

			IF "UseFullTextSearch" then
				sql_command := sql_command || 'to_tsvector(p.ShortDescription || '' '' || p.FullDescription) @@ to_tsquery($1) ';
			ELSE
				sql_command := sql_command || 'position(p.ShortDescription in $1) > 0 or position(p.FullDescription in $1) > 0 ';
			end if;

			--localized product short description
			sql_command := sql_command || format('
			UNION
			SELECT lp.EntityId
			FROM "LocalizedProperty" lp
			WHERE
				lp.LocaleKeyGroup = %L
				AND lp.LanguageId = %L
				AND lp.LocaleKey = %L' ,'Product', "LanguageId", 'ShortDescription');

			IF "UseFullTextSearch" then
				sql_command :=  sql_command || ' AND to_tsvector(lp.LocaleValue) @@ to_tsquery($1) ';
			ELSE
				sql_command := sql_command || ' AND position(lp.LocaleValue in $1) > 0 ';
			end if;

			--localized product full description
			sql_command := sql_command || format('
			UNION
			SELECT lp.EntityId
			FROM LocalizedProperty lp
			WHERE
				lp.LocaleKeyGroup = %L
				AND lp.LanguageId = %L
				AND lp.LocaleKey = %L', 'Product', "LanguageId", 'FullDescription');

			IF "UseFullTextSearch" then
				sql_command := sql_command || ' AND to_tsvector(lp.LocaleValue) @@ to_tsquery($1) ';
			ELSE
				sql_command := sql_command || ' AND position(lp.LocaleValue in $1) > 0 ';
			end if;


        end if;

        --manufacturer part number (exact match)
		IF "SearchManufacturerPartNumber" then
			sql_command := sql_command || '
			UNION
			SELECT "p."Id""
			FROM "Product" p
			WHERE p.ManufacturerPartNumber = $2 '; --$2 = original_keywords
		END if;

		--SKU (exact match)
		IF "SearchSku" then
			sql_command := sql_command || '
			UNION
			SELECT "p."Id""
			FROM "Product" p
			WHERE p.Sku = $2 ';
		END if;

        IF "SearchProductTags" then
			--product tags (exact match)
			sql_command := sql_command || '
			UNION
			SELECT pptm.Product_Id
			FROM "Product_ProductTag_Mapping" pptm INNER JOIN "ProductTag" pt ON pt.Id = pptm.ProductTag_Id
			WHERE pt.Name = $2 '
			--localized product tags
			|| format('
			UNION
			SELECT pptm.Product_Id
			FROM "LocalizedProperty" lp INNER JOIN "Product_ProductTag_Mapping" pptm ON lp.EntityId = pptm.ProductTag_Id
			WHERE
				lp.LocaleKeyGroup = %L
				AND lp.LanguageId = %L
				AND lp.LocaleKey = %L
				AND lp."LocaleValue" = $2 ', 'ProductTag', "LanguageId", 'Name');
		END if;

		EXECUTE sql_command USING keywords, original_keywords;
    end if;

    CREATE TEMP TABLE "DisplayOrderTmp"
	(
		"Id" SERIAL PRIMARY KEY NOT NULL,
		"ProductId" int NOT NULL
	);

    sql_command := '
	SELECT p."Id"
	FROM
		"Product" p';

    IF find_in_categories then
		sql_command := sql_command || '
		INNER JOIN "Product_Category_Mapping" pcm
			ON p."Id" = pcm."ProductId"';
	END if;

    IF "ManufacturerId" > 0 then
		sql_command := sql_command || '
		INNER JOIN "Product_Manufacturer_Mapping" pmm
			ON p."Id" = pmm."ProductId"';
	END if;

    IF COALESCE("ProductTagId", 0) != 0 then
		sql_command := sql_command || '
		INNER JOIN "Product_ProductTag_Mapping" pptm
			ON p."Id" = pptm."Product_Id"';
	END if;

    --searching by keywords
	IF "SearchKeywords" then
		sql_command := sql_command || '
		JOIN "KeywordProducts" kp
			ON  p."Id" = kp."ProductId"';
	END if;

    sql_command := sql_command || '
		WHERE
			NOT p."Deleted"';

    --filter by category
	IF find_in_categories then
		sql_command := sql_command || format('AND pcm."CategoryId" = ANY (string_to_array(%L, %L)::int[])', "CategoryIds", ',');

		IF "FeaturedProducts" IS NOT NULL then
			sql_command := sql_command || format('AND pcm."IsFeaturedProduct" = %L', "FeaturedProducts");
		END if;
	END if;

    --filter by manufacturer
	IF "ManufacturerId" > 0 then
		 sql_command := sql_command || format('AND pmm."ManufacturerId" = %L', "ManufacturerId");

		IF "FeaturedProducts" IS NOT NULL then
			 sql_command := sql_command || format('AND pmm."IsFeaturedProduct" = %L', "FeaturedProducts");
		END if;
	END if;

    --filter by vendor
	IF "VendorId" > 0 then
		 sql_command := sql_command || format('AND p."VendorId" = %L', "VendorId");
	END if;

    --filter by warehouse
	IF "WarehouseId" > 0 then
		--we should also ensure that 'ManageInventoryMethodId' is set to 'ManageStock' (1)
		--but we skip it in order to prevent hard-coded values (e.g. 1) and for better performance
		 sql_command := sql_command || format('
		AND
			(
				(p."UseMultipleWarehouses" = 0 AND
					p."WarehouseId" = %2$L)
				OR
				(p."UseMultipleWarehouses" > 0 AND
					EXISTS (SELECT 1 FROM "ProductWarehouseInventory" pwi
					WHERE pwi."WarehouseId" = %2$L AND pwi."ProductId" = p."Id"))
			)', "WarehouseId");
	END if;

    --filter by product type
	IF "ProductTypeId" is not null then
		 sql_command := sql_command || format('
			AND p."ProductTypeId" = %L', "ProductTypeId");
	END if;

	--filter by "visible individually"
	IF "VisibleIndividuallyOnly" then
		 sql_command := sql_command || 'AND p."VisibleIndividually"';
	END if;

	--filter by "marked as new"
	IF "MarkedAsNewOnly" then
		 sql_command := sql_command || format('
			AND p."MarkAsNew"
			AND (CURRENT_TIMESTAMP AT TIME ZONE %L BETWEEN COALESCE(p."MarkAsNewStartDateTimeUtc", %L) and COALESCE(p."MarkAsNewEndDateTimeUtc", %L))',
		     'UTC', '-infinity', 'infinity');
	END if;

    --filter by product tag
	IF COALESCE("ProductTagId", 0) != 0 then
		 sql_command := sql_command || format('
			AND pptm."ProductTag_Id" = %L', "ProductTagId");
	END if;

	--"Published" property
	IF "OverridePublished" is null then
		--process according to "showHidden"
		IF not "ShowHidden" then
			 sql_command := sql_command || '
			AND p."Published"';
		END if;
	ELSEIF "OverridePublished" then
		--published only
		 sql_command := sql_command || '
			AND p."Published"';
	ELSEIF not "OverridePublished" then
		--unpublished only
		 sql_command := sql_command || '
			AND not p."Published"';
	END if;

	--show hidden
	IF not "ShowHidden" then
		 sql_command := sql_command || format('
			AND not p."Deleted"
			AND (CURRENT_TIMESTAMP AT TIME ZONE %L BETWEEN COALESCE(p."AvailableStartDateTimeUtc", %L) and COALESCE(p."AvailableEndDateTimeUtc", %L))',
		    'UTC', '-infinity', 'infinity');
	END if;

    --min price
	IF "PriceMin" is not null then
		 sql_command := sql_command || format('
			AND (p."Price" >= %L)', "PriceMin");
	END if;

	--max price
	IF "PriceMax" is not null then
		 sql_command := sql_command || format('
		    AND (p."Price" <= %L)', "PriceMax");
	END if;

    --show hidden and ACL
	IF not "ShowHidden" and find_in_customer_roles then
		 sql_command := sql_command || format('
			AND (not p."SubjectToAcl" OR EXISTS (
					SELECT 1
					from "AclRecord" as acl
					where acl."CustomerRoleId" = ANY (string_to_array(%L, %L)::int[])
						and acl."EntityId" = p."Id" AND acl."EntityName" = %L
					)
				)', "AllowedCustomerRoleIds", ',', 'Product');
	END if;

	--filter by store
	IF "StoreId" > 0 then
		 sql_command := sql_command || format('
			AND (not p."LimitedToStores" OR EXISTS (
				SELECT 1 FROM "StoreMapping" sm
				WHERE sm."EntityId" = p."Id" AND sm."EntityName" = %L and sm."StoreId"= %L
				))', 'Product', "StoreId");
	END if;

    --prepare filterable specification attribute option identifier (if requested)
    IF "LoadFilterableSpecificationAttributeOptionIds" then
        sql_filterableSpecs :=
            'SELECT array_agg(DISTINCT psam."SpecificationAttributeOptionId")
            FROM "Product_SpecificationAttribute_Mapping" psam
            WHERE psam."AllowFiltering"
            AND psam."ProductId" IN (' || sql_command || ')';

		EXECUTE sql_filterableSpecs into filterable_specs;
        GET DIAGNOSTICS filterable_specs_count = ROW_COUNT;

		--build comma separated list of filterable identifiers
        if filterable_specs_count > 0 then
            "FilterableSpecificationAttributeOptionIds" :=
                CASE WHEN "FilterableSpecificationAttributeOptionIds" IS NULL THEN
                    array_to_string(filterable_specs, ',')
                ELSE
                    "FilterableSpecificationAttributeOptionIds" || ',' || array_to_string(filterable_specs, ',')
                END;
        end if;
    end if;

    --filter by specification attribution options
    IF find_in_specs then
		 sql_command := sql_command || format( '
			AND (p."Id" in (
					select psa."ProductId"
                    from "Product_SpecificationAttribute_Mapping" as psa
                    where psa."SpecificationAttributeOptionId" ANY (string_to_array(%L, %L)::int[]) and psa."AllowFiltering"
                )
			)', "FilteredSpecs", ',');
    end if;

	--sorting
    sql_orderby := '';

    CASE "OrderBy"
	WHEN 5 THEN sql_orderby := ' p."Name" ASC'; /* Name: A to Z */
	WHEN 6 THEN sql_orderby := ' p."Name" DESC'; /* Name: Z to A */
	WHEN 10 THEN sql_orderby := ' p."Price" ASC'; /* Price: Low to High */
	WHEN 11 THEN sql_orderby := ' p."Price" DESC'; /* Price: High to Low */
	WHEN 15 THEN sql_orderby := ' p."CreatedOnUtc" DESC'; /* creation date */
	ELSE /* default sorting, 0 (position) */
		begin
			IF find_in_categories then
				sql_orderby := ' pcm."DisplayOrder" ASC';
			end if;

			--manufacturer position (display order)
			IF "ManufacturerId" > 0 then
				IF length(sql_orderby) > 0 then
					sql_orderby := sql_orderby || ', ';
				end if;
				sql_orderby :=  sql_orderby || ' pmm."DisplayOrder" ASC';
			END if;

			--name
			IF length(sql_orderby) > 0 then
				sql_orderby := sql_orderby || ', ';
			end if;
			sql_orderby := sql_orderby ||  ' p."Name" ASC';
        end;
	end case;

    sql_command := sql_command || '
	    ORDER BY' || sql_orderby;

    sql_command := '
    INSERT INTO "DisplayOrderTmp" ("ProductId") ' || sql_command;

    --SELECT sql_command; --debug

	EXECUTE sql_command;

    PERFORM "Id" FROM "DisplayOrderTmp";

    GET DIAGNOSTICS "TotalRecords" := row_count;

    "Products" := (select json_agg(sub)
    from (select *
	    FROM "DisplayOrderTmp" dot
		INNER JOIN "Product" p on p."Id" = dot."ProductId"
	WHERE dot."Id" > "PageSize" * "PageIndex"
	ORDER BY dot."Id"
    limit "PageSize") sub);

    drop TABLE if exists "KeywordProducts";
    drop TABLE if exists "DisplayOrderTmp";

    --return products
	RETURN ROW("Products", "FilterableSpecificationAttributeOptionIds", "TotalRecords")::"TableFunctionResult";


END;
$BODY$;